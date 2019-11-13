using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nano.Storage.Common
{
	public interface IFixedSizeRandomAccessDevice
	{
		long Length { get; }
		bool CanWrite { get; }
		int Read(long pos, byte[] buffer, int offset, int count);
		void Write(long pos, byte[] buffer, int offset, int count);
		void Flush();
		void Close();
	}

	public class FixedSizeStream : Stream
	{
		IFixedSizeRandomAccessDevice m_device;
		long m_pos, m_len;

		public FixedSizeStream(IFixedSizeRandomAccessDevice device)
		{
			m_device = device;
			m_pos = 0;
			m_len = device.Length;
		}

		#region Properties

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return m_device.CanWrite; }
		}

		public override long Length
		{
			get { return m_len; }
		}

		public override long Position
		{
			get
			{
				return m_pos;
			}
			set
			{
				if (value < 0 || value > m_len)
					throw new ArgumentOutOfRangeException();
				m_pos = value;
			}
		}

		#endregion

		public override long Seek(long offset, SeekOrigin origin)
		{
			long newpos;
			switch (origin)
			{
				case SeekOrigin.Begin:
					newpos = offset;
					break;
				case SeekOrigin.Current:
					newpos = m_pos + offset;
					break;
				case SeekOrigin.End:
					newpos = m_len + offset;
					break;
				default:
					throw new InvalidOperationException();
			}
			return Position = newpos;
		}

		public override void SetLength(long value)
		{
			if (value != m_len)
				throw new AccessViolationException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			count = (int)Math.Min(count, m_len - m_pos);
			int cbRead = m_device.Read(m_pos, buffer, offset, count);
			m_pos += cbRead;
			return cbRead;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (m_pos + count > m_len)
				throw new ArgumentOutOfRangeException();
			m_device.Write(m_pos, buffer, offset, count);
			m_pos += count;
		}

		public override void Flush()
		{
			m_device.Flush();
		}

		public override void Close()
		{
			base.Close();
			m_device.Close();
			m_device = null;
		}
	}

	public class ByteArrayRandomAccessDevice : IFixedSizeRandomAccessDevice
	{
		byte[] m_buffer;
		bool m_writable;

		public ByteArrayRandomAccessDevice(int size, bool writable)
		{
			m_buffer = new byte[size];
			m_writable = writable;
		}

		public ByteArrayRandomAccessDevice(byte[] buffer, bool writable)
		{
			m_buffer = buffer;
			m_writable = writable;
		}

		public long Length
		{
			get { return m_buffer.Length; }
		}

		public bool CanWrite
		{
			get { return m_writable; }
		}

		public int Read(long pos, byte[] buffer, int offset, int count)
		{
			if (pos < 0 || count < 0 || pos + count > m_buffer.Length)
				throw new ArgumentOutOfRangeException();
			Array.Copy(m_buffer, (int)pos, buffer, offset, count);
			return count;
		}

		public void Write(long pos, byte[] buffer, int offset, int count)
		{
			if (!m_writable)
				throw new AccessViolationException();
			if (pos < 0 || count < 0 || pos + count > m_buffer.Length)
				throw new ArgumentOutOfRangeException();
			Array.Copy(buffer, offset, m_buffer, (int)pos, count);
		}

		public void Flush() { }

		public void Close() { }

		public static Stream CreateStream(int size, bool writable)
		{
			return CreateStream(new byte[size], writable);
		}

		public static Stream CreateStream(byte[] buffer, bool writable)
		{
			ByteArrayRandomAccessDevice device = new ByteArrayRandomAccessDevice(buffer, writable);
			return new FixedSizeStream(device);
		}
	}
}
