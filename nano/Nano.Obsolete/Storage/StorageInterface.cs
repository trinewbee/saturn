using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Common;

namespace Nano.Storage.Backward
{
	// deprecated
	public enum RewriteCapability
	{
		/// <summary>The object's size can be changed</summary>
		CanResize = 1,

		/// <summary>The object can be reopened to write</summary>
		CanAppend = 2,

		/// <summary>The object can be reopened to append</summary>
		CanWrite = 4,

		/// <summary>The object must not be resized nor writable</summary>
		AtomBehavior = 0,

		/// <summary>The object can be re-opened for appending but existing data is read-only</summary>
		AppendBehavior = CanResize | CanAppend,

		/// <summary>The object can be re-opened for writing and resizing</summary>
		WritableBehavior = CanResize | CanAppend | CanWrite,
	}

	public enum DataOpenMode
	{
		/// <summary>The object is opened for reading</summary>
		Read,

		/// <summary>The object is opened for appending</summary>
		/// <remarks>At this mode, the whole file is readable</remarks>
		Append,

		/// <summary>The object is opened for reading and writing</summary>
		ReadWrite
	}

	public interface IKeyValueStorage
	{
		RewriteCapability RewriteCaps { get; }

		IEnumerable<BinaryValue> ListKeys();

		bool HasKey(BinaryValue key);

		/// <summary>Create a new file object</summary>
		/// <param name="key">Key of file</param>
		/// <param name="size">Initial size</param>
		/// <returns>
		/// For a storage does not support CanResize capability, the size must
		/// be the actual data size and can not be changed.
		/// For a storage supports CanResize capability, the size is a hint
		/// while the actual size depends on Write / SetLength operations.
		/// </returns>
		Stream CreateFile(BinaryValue key, long size);

		Stream OpenFile(BinaryValue key, DataOpenMode mode);
	}

	public enum StorageCapability
	{
		/// <summary>Support versions feature</summary>
		SupportVersions = 1,

		/// <summary>Support modifying files</summary>
		SupportModification = 2,

		/// <summary>Support appending files</summary>
		/// <remarks>
		/// If this flag is not set, the implementation supports only atom submission
		/// when creating or modifying files (if SupportModification set).
		/// That is, you must pass the the whole source data, and none writable stream
		/// would be returned.
		/// </remarks>
		SupportAppending = 4,

		/// <summary>Support case sensitive on file and folder name</summary>
		SupportCaseSensitive = 8,

		/// <summary>Support data encryption</summary>
		SupportCryption = 0x10,

		/// <summary>SHA1 hash stored</summary>
		/// <remarks>SHA1 of file is stored and could be gotten without recomputing</remarks>
		QuickSHA1Info = 0x100,

		/// <summary>MD5 hash stored</summary>
		/// <remarks>MD5 of file is stored and could be gotten without recomputing</remarks>
		QuickMD5Info = 0x200,

		/// <summary>SHA1 Mapping Storage</summary>
		/// <remarks>
		/// The implementation stores files by a key of SHA1 hash code.
		/// Which means, if two files are identical, only one copy will be stored, to reduce the total space used.
		/// And you should specify the correct SHA1 hash when creating a new file.
		/// After you complete writing your file, the file would be checked whether it's identical to the hash code you specified,
		/// and will fail to be submitted if not.
		/// If this feature is on, QuickSHA1Info is also on, and either SupportModification or SupportChangingSize should be off.
		/// </remarks>
		SHA1Mapping = 0x1000,

		/// <summary>MD5 Mapping Storage</summary>
		/// <remarks>See also: SHA1Mapping</remarks>
		MD5Mapping = 0x2000,
	}

	public enum StorageEntryType
	{
		File,
		Folder
	}

	public interface IStorageEntry
	{
		/// <summary>Parent of this entry</summary>
		IStorageFolder Parent { get; }

		/// <summary>Get the type of this entry</summary>
		StorageEntryType Type { get; }

		/// <summary>Get the name of entry</summary>
		string Name { get; }

		/// <summary>Get and set the time created</summary>
		DateTime CreationTimeUtc { get; set; }

		/// <summary>Get and set the time last written</summary>
		DateTime LastWriteTimeUtc { get; set; }
	}

	public interface IStorageFile: IStorageEntry
	{
		/// <summary>Get the size of file</summary>
		long Length { get; }

		/// <summary>Open the file as a stream</summary>
		Stream Open(bool fWrite);
	}

	public interface IStorageFolder: IStorageEntry
	{
		/// <summary>Get number of files</summary>
		int FileCount { get; }

		/// <summary>Get number of sub-folders</summary>
		int SubFolderCount { get; }

		/// <summary>Get item of given name</summary>
		IStorageEntry this[string name] { get; }

		/// <summary>Get entries (files and sub-folders)</summary>
		IEnumerable<IStorageEntry> Entries { get; }

		/// <summary>Get files</summary>
		IEnumerable<IStorageFile> Files { get; }

		/// <summary>Get sub-folders</summary>
		IEnumerable<IStorageFolder> SubFolders { get; }

		/// <summary>Create a new file</summary>
		Stream CreateFile(string name, out IStorageFile file);

		/// <summary>Create a new file and submit it atomicly</summary>
		/// <remarks>
		/// The hash code would be checked after data copied.
		/// If the given hash code is not identical to the data, the operation failed.
		/// </remarks>
		IStorageFile CreateFileAtom(string name, byte[] hash, Stream streamSource);

		/// <summary>Create a new folder</summary>
		IStorageFolder CreateFolder(string name, bool fIgnoreExisting);
	}

	public interface IStorageVersion
	{
		/// <summary>Get owner library</summary>
		IStorageLibrary Library { get; }

		/// <summary>Get time of this version</summary>
		DateTime TimestampUtc { get; }

		/// <summary>Root folder</summary>
		IStorageFolder Root { get; }
	}

	public interface IStorageLibrary
	{
		/// <summary>Get capability</summary>
		StorageCapability Capability { get; }

		/// <summary>Largest file size supported</summary>
		long MaxFileLength { get; }

		/// <summary>Get latest version</summary>
		IStorageVersion Storage { get; }

		/// <summary>Get versions</summary>
		/// <remarks>Return null if versions not supported</remarks>
		IList<IStorageVersion> Versions { get; }

		/// <summary>Open the library</summary>
		void Open();

		/// <summary>Close the library</summary>
		void Close();
	}

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
