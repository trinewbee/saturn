using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Nano.Crypt
{
	/// <summary>Interface of block transformer</summary>
	/// <remarks>
	/// Used in a crypt method that all blocks are independently encrypted.
	/// </remarks>
	public interface IBlockTransform
	{
		/// <summary>Block size of the algorithm, in bytes</summary>
		int BlockSize { get; }

		/// <summary>It is an ECB transform</summary>
		/// <remarks>ECB transform treats blocks indenpendently, so a random operation could take</remarks>
		bool IsEcb { get; }

		/// <summary>Transform data from array s to array t</summary>
		/// <remarks>
		/// Length of data should be multiple of block size, otherwise an exception
		/// will be raised.
		/// </remarks>
		void Transform(byte[] s, int offs, byte[] t, int offt, int len);
	}

	public interface ITailTransform
	{
		/// <summary>Transform the last block</summary>
		/// <remarks>
		/// len should be not greater than block size. If equal, it has the same result of Transform
		/// </remarks>
		byte[] TransformTail(byte[] s, int off, int len);
	}

	public class BlockTransform : IBlockTransform
	{
		ICryptoTransform m_t;
		int m_blockSize;
		bool m_isEcb;

		public BlockTransform(ICryptoTransform t, bool isEcb)
		{
			if (!t.CanTransformMultipleBlocks || t.InputBlockSize != t.OutputBlockSize)
				throw new NotSupportedException("ICryptoTransform not fit");

			m_t = t;
			m_blockSize = t.InputBlockSize;
			m_isEcb = isEcb;
		}

		public int BlockSize
		{
			get { return m_blockSize; }
		}

		public bool IsEcb
		{
			get { return m_isEcb; }
		}

		public void Transform(byte[] s, int offs, byte[] t, int offt, int len)
		{
			if (len == 0)
				return;
			if (len % m_blockSize != 0 || m_t.TransformBlock(s, offs, len, t, offt) != len)
				throw new CryptographicException();
		}
	}

	public class PlainTailTransform : ITailTransform
	{
		public byte[] TransformTail(byte[] s, int off, int len)
		{
			byte[] t = new byte[len];
			Array.Copy(s, off, t, 0, len);
			return t;
		}
	}

	public class XorTailTransform : ITailTransform
	{
		byte[] m_data;

		public XorTailTransform(byte[] data)
		{
			m_data = data;
		}

		public byte[] TransformTail(byte[] s, int off, int len)
		{
			byte[] t = new byte[len];
			for (int i = 0; i < len; ++i)
				t[i] = (byte)(s[i + off] ^ m_data[i]);
			return t;
		}
	}

	public class BlockCrypt
	{
		IBlockTransform m_t;
		ITailTransform m_tail;

		public BlockCrypt(IBlockTransform t, ITailTransform tail)
		{
			m_t = t;
			m_tail = tail;
		}

		public void TransformStream(Stream istream, Stream ostream)
		{
			long len = istream.Length;
			ostream.SetLength(len);

			istream.Seek(0, SeekOrigin.Begin);
			ostream.Seek(0, SeekOrigin.Begin);

			const int bufSize = 0x400000;	// 4MB
			byte[] bufS = new byte[bufSize];
			byte[] bufT = new byte[bufSize];

			while (len >= bufSize)
			{
				if (istream.Read(bufS, 0, bufSize) != bufSize)
					throw new IOException("Read failed");
				m_t.Transform(bufS, 0, bufT, 0, bufSize);
				ostream.Write(bufT, 0, bufSize);
				len -= bufSize;
			}

			int remain = (int)len;
			if (remain != 0)
			{
				if (istream.Read(bufS, 0, remain) != remain)
					throw new IOException("Read failed");
				int opalg = remain & ~0xF;
				if (opalg != 0)
				{
					m_t.Transform(bufS, 0, bufT, 0, opalg);
					ostream.Write(bufT, 0, opalg);
				}

				if (remain > opalg)
				{
					byte[] t = m_tail.TransformTail(bufS, opalg, remain - opalg);
					ostream.Write(t, 0, t.Length);
				}
			}
		}

		public byte[] ComputeHash(Stream istream, HashAlgorithm alg)
		{
			long len = istream.Length;
			istream.Seek(0, SeekOrigin.Begin);
			alg.Initialize();

			const int bufSize = 0x400000;	// 4MB
			byte[] bufS = new byte[bufSize];
			byte[] bufT = new byte[bufSize];

			while (len >= bufSize)
			{
				if (istream.Read(bufS, 0, bufSize) != bufSize)
					throw new IOException("Read failed");
				m_t.Transform(bufS, 0, bufT, 0, bufSize);
				if (alg.TransformBlock(bufT, 0, bufSize, bufT, 0) != bufSize)
					throw new CryptographicException();
				len -= bufSize;
			}

			int remain = (int)len;
			if (remain != 0)
			{
				if (istream.Read(bufS, 0, remain) != remain)
					throw new IOException("Read failed");
				int opalg = remain & ~0xF;
				if (opalg != 0)
				{
					m_t.Transform(bufS, 0, bufT, 0, opalg);
					if (alg.TransformBlock(bufT, 0, opalg, bufT, 0) != opalg)
						throw new CryptographicException();
				}

				if (remain > opalg)
				{
					byte[] t = m_tail.TransformTail(bufS, opalg, remain - opalg);
					alg.TransformFinalBlock(t, 0, t.Length);
				}
				else
					alg.TransformFinalBlock(bufT, 0, 0);
			}
			else
				alg.TransformFinalBlock(bufT, 0, 0);

			return alg.Hash;
		}

		public byte[] ReadPart(Stream istream, long pos, int length)
		{
			Debug.Assert(length >= 0);
			if (pos < 0 || pos >= istream.Length || length == 0)
				return new byte[0];

			// Aligning to 16-bytes
			long posSft = pos & ~0xFL;
			long posTft = (pos + length + 15) & ~0xFL;

			// Asure not exceeding the end of file
			if (posTft > istream.Length)
				posTft = istream.Length;

			// Reading from stream
			int op = (int)(posTft - posSft);
			byte[] sbuf = new byte[op];
			lock (istream)
			{
				istream.Seek(posSft, SeekOrigin.Begin);
				if (istream.Read(sbuf, 0, op) != op)
					throw new IOException("Read failed");
			}

			// Transform non-tail parts
			int opAlg = op & ~0xF;
			byte[] tbuf = new byte[opAlg];
			if (opAlg != 0)
				m_t.Transform(sbuf, 0, tbuf, 0, opAlg);

			// Transform tail part
			byte[] tail = m_tail.TransformTail(sbuf, opAlg, op - opAlg);
			int len_d = opAlg + tail.Length;

			// Make result
			int shift = (int)(pos - posSft);
			int returnSize = Math.Min(len_d - shift, length);	// number of bytes to be returned
			byte[] rbuf = new byte[returnSize];

			int frontSize = opAlg - shift;
			if (frontSize > 0)
			{
				Array.Copy(tbuf, shift, rbuf, 0, Math.Min(frontSize, returnSize));
				if (returnSize > frontSize)
					Array.Copy(tail, 0, rbuf, frontSize, returnSize - frontSize);
			}
			else
				Array.Copy(tail, shift, rbuf, 0, returnSize);

			return rbuf;
		}

		public delegate byte[] ReadDelegate(long pos, int size);

		/// <summary>Transform part of data</summary>
		/// <param name="f"></param>
		/// <param name="length">Full length of source</param>
		/// <param name="pos">Position</param>
		/// <param name="size">Size to be read</param>
		/// <returns>Data read</returns>
		public byte[] TransformPart(ReadDelegate f, long length, long pos, int size)
		{
			long sc = length;
			long sb = sc & ~0xFL;
			long lb = pos & ~0xFL;
			long hb = (pos + size + 0xF) & ~0xFL;
			if (hb > sb)    // assure read whole tail
				hb = sc;

			int ssize = (int)(hb - lb);
			byte[] ss = f(lb, ssize);
			Debug.Assert(ss.Length == ssize);
			byte[] st = this.TransformFinalBlock(ss);

			// fixed-tail-size
			long lbt = pos;
			long hbt = Math.Min(sc, pos + size);
			Debug.Assert(lbt >= lb && hbt <= lb + st.Length);
			byte[] sr = new byte[(int)(hbt - lbt)];
			Array.Copy(st, lbt - lb, sr, 0, sr.Length);
			return sr;
		}

		public byte[] TransformFinalBlock(byte[] data)
		{
			byte[] r = new byte[data.Length];
			int m = data.Length & ~0xF;
			m_t.Transform(data, 0, r, 0, m);
			byte[] rt = m_tail.TransformTail(data, m, data.Length - m);
			Debug.Assert(rt.Length == data.Length - m);
			Array.Copy(rt, 0, r, m, data.Length - m);
			return r;
		}
	}

	/// <summary>只读加密流</summary>
	/// <remarks>
	/// ReadonlyTransformStream 可以在一个已有的流上直接实现一个只读的对称变换流，从而直接读取加密/解密数据。
	/// 此类要求数据按照 16 字节一组的 ECB 加密（同 BlockCrypt 类），流末尾不满 16 字节部分使用 Tail Transform。
	/// </remarks>
	public class ReadonlyTransformStream : Stream
	{
		/// <summary>如果源流小于该值，CreateInstance 方法会直接返回一个内存流</summary>
		public static uint UseMemory = 0x2000000; // Keep in memory

		Stream m_inner;
		BlockCrypt m_bc;

		/// <summary>创建一个只读加密流实例</summary>
		/// <param name="inner">需要加密的流</param>
		/// <param name="bc">加密工具</param>
		/// <remarks>建议使用 CreateInstance 方法来创建只读加密流。</remarks>
		public ReadonlyTransformStream(Stream inner, BlockCrypt bc)
		{
			m_inner = inner;
			m_bc = bc;
		}

		#region Properties

		/// <summary>获取流是否可读</summary>
		public override bool CanRead => true;

		/// <summary>获取流是否可随机定位</summary>
		public override bool CanSeek => true;

		/// <summary>获取流是否可写，本类总是返回 false</summary>
		public override bool CanWrite => false;

		/// <summary>获取流的长度</summary>
		public override long Length => m_inner.Length;

		/// <summary>获取和设置当前读写位置</summary>
		public override long Position { get; set; } = 0;

		#endregion

		/// <summary>设置当前读写位置</summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					return Position = offset;
				case SeekOrigin.Current:
					return Position += offset;
				case SeekOrigin.End:
					return Position = Length + offset;
				default:
					throw new ArgumentException();
			}
		}

		/// <summary>设置流的长度，本类该函数会导致 AccessViolation 异常</summary>
		public override void SetLength(long value) => throw new AccessViolationException();

		/// <summary>读取数据</summary>
		/// <param name="buffer">保存数据的数组</param>
		/// <param name="offset">起始下标</param>
		/// <param name="count">最大长度</param>
		/// <returns>返回读取的字节数</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (Position < 0)
				throw new IndexOutOfRangeException();
			if (Position >= Length)
				return 0;

			count = (int)Math.Min(Length - Position, count);
			var data = m_bc.ReadPart(m_inner, Position, count);
			Position += data.Length;
			if (data.Length != count)
				throw new IOException("Read stream failed");

			Array.Copy(data, 0, buffer, offset, count);
			return count;
		}

		/// <summary>写入数据，本类该函数会导致 AccessViolation 异常</summary>
		/// <param name="buffer">保存数据的数组</param>
		/// <param name="offset">起始下标</param>
		/// <param name="count">数据的字节数</param>
		public override void Write(byte[] buffer, int offset, int count) => throw new AccessViolationException();

		/// <summary>将数据修改强制更新到介质</summary>
		/// <remarks>因为是只读流，该函数不做任何动作。</remarks>
		public override void Flush() { }

		/// <summary>创建一个只读加密流</summary>
		/// <param name="inner">需要加密的流</param>
		/// <param name="bc">加密工具</param>
		/// <returns>
		/// 与使用 new 创建实例不同，本方法会对长度不大于 UseMemory 的数据直接创建一个 MemoryStream，以加快小文件的访问速度。
		/// </returns>
		public static Stream CreateInstance(Stream inner, BlockCrypt bc)
		{
			if (inner.Length <= UseMemory)
			{
				var stream = new MemoryStream((int)inner.Length);
				bc.TransformStream(inner, stream);
				return stream;
			}
			else
				return new ReadonlyTransformStream(inner, bc);
		}
	}

	/// <summary>AES cryption helper class</summary>
	public class AesCryptAgent
	{
		Aes m_aes;
		ITailTransform m_tail;

		#region Constructors

		/// <summary>Build an AES context using the specified key, with ECB mode</summary>
		public AesCryptAgent(Aes aes, ITailTransform tail)
		{
			m_aes = aes;
			m_tail = tail;
		}

		public AesCryptAgent(byte[] key, ITailTransform tail)
		{
			m_aes = Aes.Create();
			m_aes.Key = key;
			m_aes.Mode = CipherMode.ECB;
			m_aes.Padding = PaddingMode.None;
			m_tail = tail;
		}

		public AesCryptAgent(byte[] key, byte[] magic)
		{
			m_aes = Aes.Create();
			m_aes.Key = key;
			m_aes.Mode = CipherMode.ECB;
			m_aes.Padding = PaddingMode.None;

			if (magic == null)
				magic = new byte[16];
			else if (magic.Length != 16)
				throw new ArgumentException("Length of magic must be 16");

			magic = m_aes.CreateEncryptor().TransformFinalBlock(magic, 0, magic.Length);
			Debug.Assert(magic.Length == 16);
			m_tail = new XorTailTransform(magic);
		}

		#endregion

		public IBlockTransform CreateEncryptTransform()
		{
			return new BlockTransform(m_aes.CreateEncryptor(), true);
		}

		public IBlockTransform CreateDecryptTransform()
		{
			return new BlockTransform(m_aes.CreateDecryptor(), true);
		}

		public BlockCrypt CreateEncrypt()
		{
			IBlockTransform t = CreateEncryptTransform();
			return new BlockCrypt(t, m_tail);
		}

		public BlockCrypt CreateDecrypt()
		{
			IBlockTransform t = CreateDecryptTransform();
			return new BlockCrypt(t, m_tail);
		}
	}
}
