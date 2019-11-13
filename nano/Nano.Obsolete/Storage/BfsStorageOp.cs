using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Nano.Common;
using Nano.Storage.Backward;

namespace Nano.Storage.Backward.Bfs
{
	// deprecated
	public class BfsStorageFile : IStorageFile
	{
		BfsStorageVersion m_version;
		BfsStorageFolder m_parent;
		string m_name;
		long m_size;
		DateTime m_tmCreation, m_tmLastWrite;
		byte[] m_digest;

		internal BfsStorageFile(BfsStorageVersion version, BfsStorageFolder parent, string name, long size, DateTime tmCreation, DateTime tmLastWrite, byte[] digest)
		{
			m_version = version;
			m_parent = parent;
			m_name = name;
			m_size = size;
			m_tmCreation = tmCreation;
			m_tmLastWrite = tmLastWrite;
			m_digest = digest;
		}

		#region Properties

		public StorageEntryType Type
		{
			get { return StorageEntryType.File; }
		}

		public IStorageFolder Parent
		{
			get { return m_parent; }
		}

		public long Length
		{
			get { return m_size; }
		}

		public string Name
		{
			get { return m_name; }
		}

		public DateTime CreationTimeUtc
		{
			get { return m_tmCreation; }
			set { m_tmCreation = value; }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_tmLastWrite; }
			set { m_tmLastWrite = value; }
		}

		#endregion

		public System.IO.Stream Open(bool fWrite)
		{
			if (fWrite)
				throw new AccessViolationException();

			if (m_size == 0)
				return ByteArrayRandomAccessDevice.CreateStream(0, false);
			else if (m_size <= BfsStorageLibrary.MAX_SMALL_OBJECT)
				return ((BfsStorageLibrary)m_version.Library).OpenFile(m_digest);
			else
				throw new NotImplementedException();
		}
	}

	public class BfsStorageFolder : IStorageFolder
	{
		BfsStorageVersion m_version;
		BfsStorageFolder m_parent;
		string m_name;
		DateTime m_tmCreation, m_tmLastWrite;

		Dictionary<string, BfsStorageFile> m_files;
		Dictionary<string, BfsStorageFolder> m_folders;

		internal BfsStorageFolder(BfsStorageVersion version, BfsStorageFolder parent, string name, DateTime tmCreation, DateTime tmLastWrite)
		{
			m_version = version;
			m_parent = parent;
			m_name = name;
			m_tmCreation = tmCreation;
			m_tmLastWrite = tmLastWrite;
			m_files = new Dictionary<string, BfsStorageFile>();
			m_folders = new Dictionary<string, BfsStorageFolder>();
		}

		#region Properties

		public StorageEntryType Type
		{
			get { return StorageEntryType.Folder; }
		}

		public IStorageFolder Parent
		{
			get { return m_parent; }
		}

		public string Name
		{
			get { return m_name; }
		}

		public DateTime CreationTimeUtc
		{
			get { return m_tmCreation; }
			set { m_tmCreation = value; }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_tmLastWrite; }
			set { m_tmLastWrite = value; }
		}

		public int FileCount
		{
			get { return m_files.Count; }
		}

		public int SubFolderCount
		{
			get { return m_folders.Count; }
		}

		public IStorageEntry this[string name]
		{
			get
			{
				name = name.ToLowerInvariant();
				BfsStorageFile file;
				if (m_files.TryGetValue(name, out file))
					return file;
				BfsStorageFolder folder;
				if (m_folders.TryGetValue(name, out folder))
					return folder;
				return null;
			}
		}

		public IEnumerable<IStorageEntry> Entries
		{
			get
			{
				foreach (BfsStorageFile file in m_files.Values)
					yield return file;
				foreach (BfsStorageFolder folder in m_folders.Values)
					yield return folder;
			}
		}

		public IEnumerable<IStorageFile> Files
		{
			get { return m_files.Values; }
		}

		public IEnumerable<IStorageFolder> SubFolders
		{
			get { return m_folders.Values; }
		}

		#endregion

		public Stream CreateFile(string name, out IStorageFile file)
		{
			throw new NotImplementedException();
		}

		public IStorageFile CreateFileAtom(string name, byte[] hash, Stream streamSource)
		{
			if (hash.Length != 20)
				throw new ArgumentException();

			HashAlgorithm hashalg = SHA1.Create();
			byte[] hash_t = hashalg.ComputeHash(streamSource);
			if (!BinaryValue.IsEqual(hash, hash_t))
				throw new IOException("Hash code not correct");

			BlockStorage blockfs = m_version.BlockFS;
			BinaryValue uid = new BinaryValue(24);
			Array.Copy(hash, uid.Data, hash.Length);
			if (!blockfs.HasKey(uid))
			{
				long remains = streamSource.Length;
				Stream streamDest = blockfs.CreateFile(uid, (int)remains);
				byte[] buffer = new byte[0x400000];
				streamSource.Position = 0;
				while (remains > buffer.Length)
				{
					if (streamSource.Read(buffer, 0, buffer.Length) != buffer.Length)
						throw new IOException("Unable to read data");
					streamDest.Write(buffer, 0, buffer.Length);
					remains -= buffer.Length;
				}
				if (streamSource.Read(buffer, 0, (int)remains) != (int)remains)
					throw new IOException("Unable to read data");
				streamDest.Write(buffer, 0, (int)remains);
				streamDest.Close();
			}

			DateTime now = DateTime.UtcNow;
			BfsStorageFile file = new BfsStorageFile(m_version, this, name, streamSource.Length, now, now, hash);
			m_files.Add(file.Name.ToLowerInvariant(), file);
			return file;
		}

		public IStorageFolder CreateFolder(string name, bool fIgnoreExisting)
		{
			if (!m_version.CanWrite)
				throw new AccessViolationException();

			throw new NotImplementedException();
		}
	}

	public class BfsStorageVersion : IStorageVersion
	{
		BfsStorageLibrary m_library;
		BlockStorage m_blockfs;
		bool m_writable;

		public BfsStorageVersion(BfsStorageLibrary library, BlockStorage blockfs, bool writable)
		{
			m_library = library;
			m_blockfs = blockfs;
			m_writable = writable;
		}

		public IStorageLibrary Library
		{
			get { return m_library; }
		}

		public BlockStorage BlockFS
		{
			get { return m_blockfs; }
		}

		public bool CanWrite
		{
			get { return m_writable; }
		}

		public DateTime TimestampUtc
		{
			get { throw new NotImplementedException(); }
		}

		public IStorageFolder Root
		{
			get { throw new NotImplementedException(); }
		}
	}

	public class BfsStorageLibrary : IStorageLibrary
	{
		public const int MAX_SMALL_OBJECT = 0x1000000;	// 16MB

		const StorageCapability CAPABILITY = 
			StorageCapability.SupportVersions | StorageCapability.SupportCryption |
			StorageCapability.QuickSHA1Info | StorageCapability.SHA1Mapping;

		string m_path;
		BlockStorage m_blocks;

		public BfsStorageLibrary(string path)
		{
			m_path = path;
			m_blocks = new BlockStorage();
		}

		public StorageCapability Capability
		{
			get { return CAPABILITY; }
		}

		public long MaxFileLength
		{
			get { return 0xFFFFFFFFL; }	// 4GB
		}

		public IStorageVersion Storage
		{
			get { throw new NotImplementedException(); }
		}

		public IList<IStorageVersion> Versions
		{
			get { throw new NotImplementedException(); }
		}

		public void Open()
		{
			BlockStorageConfig config = BlockStorageConfig.Make64M();
			m_blocks.Open(m_path, config);
		}

		public void Close()
		{
			m_blocks.Close();
		}

		internal Stream OpenFile(byte[] digest)
		{
			BinaryValue uid = new BinaryValue(24);
			Array.Copy(digest, uid.Data, digest.Length);
			return m_blocks.OpenFile(uid, DataOpenMode.Read);
		}
	}
}
