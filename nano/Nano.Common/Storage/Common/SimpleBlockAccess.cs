using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Collection;

namespace Nano.Storage.Common
{
	public class BfsSoKeyValueAccess : KeyValueAccess
	{
		class ObjectItem : ObjectInfo
		{
			public BfsBlock Block;
			public int EntryIndex;

			public ObjectItem(string name, long size, BfsBlock block, int entryIndex)
			{
				Name = name;
				Size = size;
				Block = block;
				EntryIndex = entryIndex;
			}
		}

		BfsConfig m_config;
		long m_maxsize;

		FileTreeItem m_dir;
		bool m_isRemote, m_supportStream;

		List<BfsVariantSizeBlock> m_vsbArray = null;
		Dictionary<string, ObjectItem> m_items = null;

		public BfsSoKeyValueAccess(BfsConfig config, FileTreeAccess acc, FileTreeItem dir)
		{
			m_config = config;
			m_maxsize = m_config.MaxSize;

			m_dir = dir;
			m_isRemote = acc.IsRemote;
			Debug.Assert(acc.SupportStream);
			m_supportStream = true;

			Open();
		}

		#region Properties

		public bool CanCreate
		{
			get { return true; }
		}

		public bool CanResize
		{
			get { return false; }
		}

		public bool CanAppend
		{
			get { return false; }
		}

		public bool CanModify
		{
			get { return true; }
		}

		public bool SupportStream
		{
			get { return m_supportStream; }
		}

		public bool IsRemote
		{
			get { return m_isRemote; }
		}

		public long MaxSize
		{
			get { return m_config.MaxSize; }
		}

		public string[] HashSaved
		{
			get { return null; }
		}

		public ObjectInfo this[string name]
		{
			get { return m_items[name]; }
		}

		public ObjectInfo Refresh(string name)
		{
			return m_items[name];
		}

		#endregion

		#region Bucket methods

		public List<ObjectInfo> ListObjects()
		{
			return CollectionKit.ToList<ObjectInfo>(m_items.Values);
		}

		public bool DeleteObject(string name)
		{
			throw new NotImplementedException();
		}

		public void DeleteAll()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Atom methods

		static byte[] m_zero = new byte[0];

		public byte[] AtomRead(string name, long pos, int size)
		{
			ObjectItem item = m_items[name];
			Debug.Assert(item != null);
			BfsEntry entry = item.Block.Entries[item.EntryIndex];
			if (pos >= entry.Size)
				return m_zero;
			size = Math.Min(size, (int)(entry.Size - pos));
			if (size <= 0)
				return m_zero;
			byte[] data = new byte[size];
			if (item.Block.ReadObject(item.EntryIndex, (int)pos, data, 0, data.Length) != data.Length)
				throw new IOException("Read failed");
			return data;
		}

		public byte[] AtomRead(string name)
		{
			ObjectItem item = m_items[name];
			Debug.Assert(item != null);
			BfsEntry entry = item.Block.Entries[item.EntryIndex];
			byte[] data = new byte[entry.Size];
			if (item.Block.ReadObject(item.EntryIndex, 0, data, 0, data.Length) != data.Length)
				throw new IOException("Read failed");
			return data;
		}

		public void AtomWrite(string name, long pos, byte[] data, int off, int size)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(string name, long pos, System.IO.Stream istream, int off, int size)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(string name, long pos, System.IO.Stream istream)
		{
			throw new NotImplementedException();
		}

		public ObjectInfo AtomCreate(string name, byte[] data, int off, int size)
		{
			if (m_items.ContainsKey(name))
				throw new ArgumentException("Key already exists");

			foreach (BfsBlock block in m_vsbArray)
			{

			}
			throw new NotImplementedException();
		}

		public ObjectInfo AtomCreate(string name, System.IO.Stream istream, int off, int size)
		{
			throw new NotImplementedException();
		}

		public ObjectInfo AtomCreate(string name, byte[] data)
		{
			return AtomCreate(name, data, 0, data.Length);
		}

		public ObjectInfo AtomCreate(string name, System.IO.Stream istream)
		{
			throw new NotImplementedException();
		}

		public void WalkData(string name, StorageKit.AcceptDataDelegate f)
		{
			if (m_supportStream)
			{
				Stream stream = OpenObject(name, false);
				StorageKit.WalkData(stream, f);
			}
			else
			{
				byte[] data = AtomRead(name);
				f(data, 0, data.Length);
			}
		}

		public byte[] ComputeHash(string name, string algorithm)
		{
			if (m_supportStream)
			{
				var stream = OpenObject(name, false);
				return StorageKit.ComputeHash(stream, algorithm);
			}
			else
			{
				byte[] data = AtomRead(name);
				return StorageKit.ComputeHash(data, 0, data.Length, algorithm);
			}
		}

		#endregion

		#region Stream methods

		public Tuple<ObjectInfo, Stream> CreateObject(string name, long size)
		{
			if (size < 0 || size > m_maxsize)
				throw new ArgumentOutOfRangeException("Size out of range");

			if (size == 0)
			{
				var item = new ObjectItem(name, 0, null, -1);
				return new Tuple<ObjectInfo, Stream>(item, null);
			}

			foreach (BfsBlock block in m_vsbArray)
			{
				int iEntry;
				Stream stream = block.CreateFile(name, (int)size, out iEntry);
				if (stream != null)
				{
					ObjectItem item = new ObjectItem(name, size, block, iEntry);
					m_items.Add(name, item);
					return new Tuple<ObjectInfo, Stream>(item, stream);
				}
			}

			{
				BfsVariantSizeBlock block = CreateNewVsb();
				int iEntry;
				Stream stream = block.CreateFile(name, (int)size, out iEntry);
				ObjectItem item = new ObjectItem(name, size, block, iEntry);
				m_items.Add(name, item);
				return new Tuple<ObjectInfo, Stream>(item, stream);
			}
		}

		public Stream OpenObject(string name, bool writable)
		{
			ObjectItem item = m_items[name];
			Debug.Assert(item != null);
			return item.Block.OpenFile(item.EntryIndex, writable);
		}

		#endregion

		#region Block operation

		static int GetFileIndex(string name)
		{
			Debug.Assert(name[0] == 'V');   // VSB
			return Convert.ToInt32(name.Substring(1));
		}

		void Open()
		{
			m_vsbArray = new List<BfsVariantSizeBlock>();
			m_items = new Dictionary<string, ObjectItem>();

			List<FileTreeItem> files = m_dir.List();
			foreach (FileTreeItem item in files)
			{
				int index = GetFileIndex(item.Name);
				OpenVsb(index);
			}
		}

		void OpenVsb(int idx)
		{
			string name = "V" + idx.ToString();
			FileTreeItem file = m_dir[name];
			Debug.Assert(file != null);

			while (m_vsbArray.Count <= idx)
				m_vsbArray.Add(null);

			Debug.Assert(m_vsbArray[idx] == null);
			BfsVariantSizeBlock block = new BfsVariantSizeBlock();
			block.Open(file, m_config);
			m_vsbArray[idx] = block;

			foreach (BfsEntry entry in block.Entries)
				if (entry != null)
					AddItem(block, entry);
		}

		void AddItem(BfsBlock block, BfsEntry e)
		{
			ObjectItem fi = new ObjectItem(e.Name, e.Size, block, e.Index);
			m_items.Add(e.Name, fi);
		}

		public void Close()
		{
			m_items.Clear();
			foreach (BfsVariantSizeBlock block in m_vsbArray)
				block.Close();
			m_vsbArray = null;
		}

		BfsVariantSizeBlock CreateNewVsb()
		{
			int i = 0;
			for (; i < m_vsbArray.Count; ++i)
				if (m_vsbArray[i] == null)
					break;
			if (i == m_vsbArray.Count)
				m_vsbArray.Add(null);

			BfsVariantSizeBlock block = new BfsVariantSizeBlock();
			block.Create(m_dir, "V" + i.ToString(), m_config);
			return m_vsbArray[i] = block;
		}

		#endregion
	}
}
