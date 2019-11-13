using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nano.Storage.Backward
{
	// deprecated
	public class FileSystemStorageFile : IStorageFile
	{
		FileInfo m_fi;
		FileSystemStorageFolder m_parent;

		internal FileSystemStorageFile(FileInfo fi, FileSystemStorageFolder parent)
		{
			m_fi = fi;
			m_parent = parent;
		}

		public IStorageFolder Parent
		{
			get { return m_parent; }
		}

		public StorageEntryType Type
		{
			get { return StorageEntryType.File; }
		}

		public string Name
		{
			get { return m_fi.Name; }
		}

		public long Length
		{
			get { return m_fi.Length; }
		}

		public DateTime CreationTimeUtc
		{
			get { return m_fi.CreationTimeUtc; }
			set { m_fi.CreationTimeUtc = value; }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_fi.LastWriteTimeUtc; }
			set { m_fi.LastWriteTimeUtc = value; }
		}

		public System.IO.Stream Open(bool fWrite)
		{
			return new FileStream(m_fi.FullName, FileMode.Open,
				fWrite ? FileAccess.ReadWrite : FileAccess.Read,
				FileShare.Read);
		}
	}

	public class FileSystemStorageFolder : IStorageFolder
	{
		DirectoryInfo m_fi;
		FileSystemStorageFolder m_parent;
		Dictionary<string, FileSystemStorageFile> m_files;
		Dictionary<string, FileSystemStorageFolder> m_folders;

		internal FileSystemStorageFolder(DirectoryInfo fi, FileSystemStorageFolder parent)
		{
			m_fi = fi;
			m_parent = parent;

			m_files = new Dictionary<string, FileSystemStorageFile>();
			foreach (FileInfo subfi in fi.GetFiles())
			{
				FileSystemStorageFile fs = new FileSystemStorageFile(subfi, this);
				m_files.Add(subfi.Name.ToLowerInvariant(), fs);
			}

			m_folders = new Dictionary<string, FileSystemStorageFolder>();
			foreach (DirectoryInfo subfi in fi.GetDirectories())
			{
				FileSystemStorageFolder fs = new FileSystemStorageFolder(subfi, this);
				m_folders.Add(subfi.Name.ToLowerInvariant(), fs);
			}
		}

		#region Properties

		public IStorageFolder Parent
		{
			get { return m_parent; }
		}

		public StorageEntryType Type
		{
			get { return StorageEntryType.Folder; }
		}

		public string Name
		{
			get { return m_fi.Name; }
		}

		public DateTime CreationTimeUtc
		{
			get { return m_fi.CreationTimeUtc; }
			set { m_fi.CreationTimeUtc = value; }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_fi.LastWriteTimeUtc; }
			set { m_fi.LastWriteTimeUtc = value; }
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
				FileSystemStorageFile file;
				if (m_files.TryGetValue(name, out file))
					return file;
				FileSystemStorageFolder folder;
				if (m_folders.TryGetValue(name, out folder))
					return folder;
				return null;
			}
		}

		public IEnumerable<IStorageEntry> Entries
		{
			get
			{
				foreach (FileSystemStorageFile file in m_files.Values)
					yield return file;
				foreach (FileSystemStorageFolder folder in m_folders.Values)
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

		// Both size and hash are ignored for this implementation
		public Stream CreateFile(string name, out IStorageFile file)
		{
			FileInfo fiSub = new FileInfo(Path.Combine(m_fi.FullName, name));
			Stream stream = new FileStream(fiSub.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
			FileSystemStorageFile fileimpl = new FileSystemStorageFile(fiSub, this);
			m_files.Add(name.ToLowerInvariant(), fileimpl);
			file = fileimpl;
			return stream;
		}

		public IStorageFile CreateFileAtom(string name, byte[] hash, Stream streamSource)
		{
			throw new NotImplementedException();
		}

		public IStorageFolder CreateFolder(string name, bool fIgnoreExisting)
		{
			string nameKey = name.ToLowerInvariant();
			FileSystemStorageFolder folderimpl;
			if (m_folders.TryGetValue(nameKey, out folderimpl))
			{
				if (fIgnoreExisting)
					return folderimpl;
				else
					throw new IOException("Directory already exists");
			}

			DirectoryInfo diSub = m_fi.CreateSubdirectory(name);
			folderimpl = new FileSystemStorageFolder(diSub, this);
			m_folders.Add(name.ToLowerInvariant(), folderimpl);
			return folderimpl;
		}

		public void Close()
		{
			foreach (FileSystemStorageFolder folder in m_folders.Values)
				folder.Close();
			m_folders.Clear();
			m_files.Clear();
		}
	}

	public class FileSystemStorageVersion : IStorageVersion
	{
		FileSystemStorageLibrary m_library;
		FileSystemStorageFolder m_folder;

		internal FileSystemStorageVersion(FileSystemStorageLibrary library)
		{
			m_library = library;
			DirectoryInfo di = new DirectoryInfo(m_library.Path);
			m_folder = new FileSystemStorageFolder(di, null);
		}

		public IStorageLibrary Library
		{
			get { return m_library; }
		}

		public DateTime TimestampUtc
		{
			get { return DateTime.UtcNow; }
		}

		public IStorageFolder Root
		{
			get { return m_folder; }
		}

		public void Close()
		{
			m_folder.Close();
			m_folder = null;
		}
	}

	public class FileSystemStorageLibrary: IStorageLibrary
	{
		string m_path;
		FileSystemStorageVersion m_version = null;

		public FileSystemStorageLibrary(string path)
		{
			m_path = path;
		}

		public StorageCapability Capability
		{
			get { return StorageCapability.SupportModification; }
		}

		public long MaxFileLength
		{
			get { return 0xFFFFFFFFL; }	// 4GB
		}

		public IStorageVersion Storage
		{
			get { return m_version; }
		}

		public IList<IStorageVersion> Versions
		{
			get { throw new NotImplementedException(); }
		}

		public string Path
		{
			get { return m_path; }
		}

		public void Open()
		{
			m_version = new FileSystemStorageVersion(this);
		}

		public void Close()
		{
			m_version.Close();
		}
	}
}
