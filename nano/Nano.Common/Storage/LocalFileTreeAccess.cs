using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Collection;

namespace Nano.Storage
{
	class LocalFileTreeItem : FileTreeItem
	{
		LocalFileTreeAccess m_access;
		LocalFileTreeItem m_parent;
		FileSystemInfo m_fsi;

		internal LocalFileTreeItem(LocalFileTreeAccess access, LocalFileTreeItem parent, FileSystemInfo info)
		{
			m_access = access;
			m_parent = parent;
			m_fsi = info;
		}

		#region Properties

		public string Name
		{
			get { return m_fsi.Name; }
		}

		public bool IsDir
		{
			get { return (m_fsi.Attributes & FileAttributes.Directory) != 0; }
		}

		public long Size
		{
			get { return m_access.GetStreamSize(m_fsi); }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_fsi.LastWriteTimeUtc; }
			set { m_fsi.LastWriteTimeUtc = value; }
		}

		public FileTreeItem Parent
		{
			get { return m_parent; }
		}

		#endregion

		#region Atom read / write operations

		public byte[] AtomRead()
		{
			var r = m_access.RetrieveStream(m_fsi);
			var data = StorageKit.AtomRead(r.Value);
			m_access.ReturnStream(r.Key);
			return data;
		}

		public byte[] AtomRead(long pos, int size)
		{
			var r = m_access.RetrieveStream(m_fsi);
			var data = StorageKit.AtomRead(r.Value, pos, size);
			m_access.ReturnStream(r.Key);
			return data;
		}

		public void AtomWrite(long pos, byte[] data, int off, int size)
		{
			var r = m_access.RetrieveStream(m_fsi);
			StorageKit.AtomWrite(r.Value, pos, data, off, size);
			m_access.ReturnStream(r.Key);
		}

		public void AtomWrite(long pos, Stream istream)
		{
			var r = m_access.RetrieveStream(m_fsi);
			StorageKit.AtomWrite(r.Value, pos, istream);
			m_access.ReturnStream(r.Key);
		}

		public void AtomWrite(long pos, Stream istream, long off, long size)
		{
			var r = m_access.RetrieveStream(m_fsi);
			StorageKit.AtomWrite(r.Value, pos, istream, off, size);
			m_access.ReturnStream(r.Key);
		}

		public FileTreeItem AtomCreateChild(string name, byte[] data, int off, int size)
		{
			FileInfo subfi = new FileInfo(Path.Combine(m_fsi.FullName, name));
			using (Stream ostream = subfi.Create())
			{
				StorageKit.AtomWrite(ostream, 0, data, off, size);
				return new LocalFileTreeItem(m_access, this, subfi);
			}
		}

		public FileTreeItem AtomCreateChild(string name, Stream istream, long off, long size)
		{
			FileInfo subfi = new FileInfo(Path.Combine(m_fsi.FullName, name));
			using (Stream ostream = subfi.Create())
			{
				ostream.SetLength(size);
				StorageKit.AtomWrite(ostream, 0, istream, off, size);
				return new LocalFileTreeItem(m_access, this, subfi);
			}
		}

		public FileTreeItem AtomCreateChild(string name, byte[] data)
		{
			FileInfo subfi = new FileInfo(Path.Combine(m_fsi.FullName, name));
			using (Stream ostream = subfi.Create())
			{
				StorageKit.AtomWrite(ostream, 0, data, 0, data.Length);
				return new LocalFileTreeItem(m_access, this, subfi);
			}
		}

		public FileTreeItem AtomCreateChild(string name, Stream istream)
		{
			FileInfo subfi = new FileInfo(Path.Combine(m_fsi.FullName, name));
			using (Stream ostream = subfi.Create())
			{
				ostream.SetLength(istream.Length);
				StorageKit.AtomWrite(ostream, 0, istream);
				return new LocalFileTreeItem(m_access, this, subfi);
			}
		}

		public void WalkData(StorageKit.AcceptDataDelegate f)
		{
			var r = m_access.RetrieveStream(m_fsi);
			r.Value.Seek(0, SeekOrigin.Begin);
			StorageKit.WalkData(r.Value, f);
			m_access.ReturnStream(r.Key);
		}

		public byte[] ComputeHash(string algorithm)
		{
			var r = m_access.RetrieveStream(m_fsi);
			r.Value.Seek(0, SeekOrigin.Begin);
			var data = StorageKit.ComputeHash(r.Value, algorithm);
			m_access.ReturnStream(r.Key);
			return data;
		}

		#endregion

		public Tuple<FileTreeItem, Stream> CreateChild(string name, long size)
		{
			FileInfo subfi = new FileInfo(Path.Combine(m_fsi.FullName, name));
			LocalFileTreeItem subitem = new LocalFileTreeItem(m_access, this, subfi);
			Stream ostream = subfi.Create();
			return new Tuple<FileTreeItem, Stream>(subitem, ostream);
		}

		public Stream Open(bool writable)
		{
			FileAccess access = writable ? FileAccess.ReadWrite : FileAccess.Read;
			return ((FileInfo)m_fsi).Open(FileMode.Open, access, FileShare.ReadWrite);
		}

		public List<FileTreeItem> List()
		{
			if (!IsDir)
				throw new IOException("Not a directory");

			List<FileTreeItem> rs = new List<FileTreeItem>();
			FileSystemInfo[] subfis = ((DirectoryInfo)m_fsi).GetFileSystemInfos();
			foreach (FileSystemInfo subfi in subfis)
				rs.Add(new LocalFileTreeItem(m_access, this, subfi));
			return rs;
		}

		public FileTreeItem this[string name]
		{
			get
			{
				if (!IsDir)
					throw new IOException("Not a directory");
				FileSystemInfo[] subfis = ((DirectoryInfo)m_fsi).GetFileSystemInfos(name);
				Debug.Assert(subfis.Length <= 1);
				return subfis.Length != 0 ? new LocalFileTreeItem(m_access, this, subfis[0]) : null;
			}
		}

		public void Delete(bool recursive)
		{
			m_access.CloseStreamInDir(m_fsi);
			if (IsDir)
				((DirectoryInfo)m_fsi).Delete(recursive);
			else
				m_fsi.Delete();
		}

		public void DeleteChildren()
		{
			if (!IsDir)
				throw new IOException("Not a directory");

			m_access.CloseStreamInDir(m_fsi);
			DirectoryInfo di = (DirectoryInfo)m_fsi;
			foreach (DirectoryInfo subdi in di.GetDirectories())
				subdi.Delete(true);
			foreach (FileInfo subfi in di.GetFiles())
				subfi.Delete();
		}

		public void MoveTo(FileTreeItem parent, string name)
		{
			string pathNew = Path.Combine(((LocalFileTreeItem)parent).m_fsi.FullName, name);
			if (IsDir)
				((DirectoryInfo)m_fsi).MoveTo(pathNew);
			else
				((FileInfo)m_fsi).MoveTo(pathNew);
			this.m_parent = (LocalFileTreeItem)parent;
		}

		public FileTreeItem CreateDir(string name)
		{
			DirectoryInfo subdi = ((DirectoryInfo)m_fsi).CreateSubdirectory(name);
			return new LocalFileTreeItem(m_access, this, subdi);
		}

		public void Refresh()
		{
			m_fsi.Refresh();
		}
	}

	public class LocalFileTreeAccess : FileTreeAccess
	{
		string m_path;
		LocalFileTreeItem m_root;
		LRUCachePool<string, Stream> m_pool;

		public LocalFileTreeAccess(string path)
		{
			m_path = path;
			DirectoryInfo di = new DirectoryInfo(path);
			m_root = new LocalFileTreeItem(this, null, di);
			m_pool = new LRUCachePool<string, Stream>(256);
			m_pool.OnObjectObsoleted = OnStreamObjectObsoleted;
			m_pool.CreateObject = CreateStreamObject;
		}

		#region Properties

		public string Path
		{
			get { return m_path; }
		}

		public bool CanCreate
		{
			get { return true; }
		}

		public bool CanResize
		{
			get { return true; }
		}

		public bool CanAppend
		{
			get { return true; }
		}

		public bool CanModify
		{
			get { return true; }
		}

		public bool SupportStream
		{
			get { return true; }
		}

		public bool IsRemote
		{
			get { return false; }
		}

		public long MaxSize
		{
			get { return Int64.MaxValue; }
		}

		public string[] HashSaved
		{
			get { return null; }
		}

		public FileTreeItem Root
		{
			get { return m_root; }
		}

        #endregion

        public void Close()
        {
			m_pool.Dispose();
            m_path = null;
            m_root = null;
        }

		#region Stream object pool

		Stream CreateStreamObject(string path)
		{
			return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
		}

		void OnStreamObjectObsoleted(string path, Stream stream)
		{
			stream.Close();
		}

		internal KeyValuePair<string, Stream> RetrieveStream(FileSystemInfo fsi)
		{
			Debug.Assert(fsi is FileInfo && fsi.Exists);
			string path = fsi.FullName;
			Stream stream = m_pool.RetrieveForce(path);
			return new KeyValuePair<string, Stream>(path, stream);
		}

		internal void ReturnStream(string key) => m_pool.Return(key);

		internal long GetStreamSize(FileSystemInfo fsi)
		{
			Debug.Assert(fsi is FileInfo && fsi.Exists);
			string path = fsi.FullName;
			var stream = m_pool.Retrieve(path);
			if (stream != null)
			{
				long size = stream.Length;
				m_pool.Return(path);
				return size;
			}
			else
				return ((FileInfo)fsi).Length;
		}

		internal void CloseStreamInDir(FileSystemInfo fsi)
		{
			string path = fsi.FullName;
			var keys = m_pool.GetKeys();
			keys = CollectionKit.Select(keys, x => x.StartsWith(path));
			CollectionKit.WalkGeneral(keys, x => m_pool.Retire(x));
		}

		#endregion
	}
}
