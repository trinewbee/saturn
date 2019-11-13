using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Common;
using Nano.Collection;

namespace Nano.Storage
{
	public interface SimpleKeyValueLocator
	{
		IEnumerable<ObjectInfo> List(FileTreeItem root);

		void InitBuckets(FileTreeItem root);

		void DeleteObjects(FileTreeItem root);

		FileTreeItem GetParent(FileTreeItem root, string name);

		FileTreeItem GetObject(FileTreeItem root, string name);
	}

	public class SimpleKeyValueAccess : KeyValueAccess
	{
		FileTreeAccess m_acc;
		FileTreeItem m_root;
		SimpleKeyValueLocator m_loc;
		Dictionary<string, ObjectInfo> m_objects;

		#region Initialize

		public SimpleKeyValueAccess(FileTreeAccess access, FileTreeItem root, SimpleKeyValueLocator locator)
		{
			m_acc = access;
			m_root = root;
			m_loc = locator;

			m_objects = new Dictionary<string, ObjectInfo>();
			foreach (ObjectInfo obj in m_loc.List(root))
				m_objects.Add(obj.Name, obj);
		}

		#endregion

		#region Properties

		public bool CanCreate
		{
			get { return m_acc.CanCreate; }
		}

		public bool CanResize
		{
			get { return m_acc.CanResize; }
		}

		public bool CanAppend
		{
			get { return m_acc.CanAppend; }
		}

		public bool CanModify
		{
			get { return m_acc.CanModify; }
		}

		public bool SupportStream
		{
			get { return m_acc.SupportStream; }
		}

		public bool IsRemote
		{
			get { return m_acc.IsRemote; }
		}

		public long MaxSize
		{
			get { return m_acc.MaxSize; }
		}

		public string[] HashSaved
		{
			get { return m_acc.HashSaved; }
		}

		#endregion

		#region Dir methods

		public List<ObjectInfo> ListObjects()
		{
			Debug.Assert(m_objects != null);
			return CollectionKit.ToList(m_objects.Values);
		}

		public ObjectInfo this[string name]
		{
			get
			{
				Debug.Assert(m_objects != null);
				ObjectInfo value;
				if (m_objects.TryGetValue(name, out value))
					return value;
				else
					return null;
			}
		}

		public bool DeleteObject(string name)
		{
			Debug.Assert(m_objects != null);
			if (m_objects.ContainsKey(name))
			{
				FileTreeItem item = m_loc.GetObject(m_root, name);
				item.Delete(false);
				m_objects.Remove(name);
				return true;
			}
			else
				return false;
		}

		public void DeleteAll()
		{
			Debug.Assert(m_objects != null);
			m_loc.DeleteObjects(m_root);
			m_objects.Clear();
		}

		#endregion

		#region Atom read / write methods

		public byte[] AtomRead(string name, long pos, int size)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			return item.AtomRead(pos, size);
		}

		public byte[] AtomRead(string name)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			return item.AtomRead();
		}

		public void AtomWrite(string name, long pos, byte[] data, int off, int size)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			item.AtomWrite(pos, data, off, size);
			m_objects[name].Size = item.Size;
		}

		public void AtomWrite(string name, long pos, Stream istream, int off, int size)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			item.AtomWrite(pos, istream, off, size);
			m_objects[name].Size = item.Size;
		}

		public void AtomWrite(string name, long pos, Stream istream)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			item.AtomWrite(pos, istream);
			m_objects[name].Size = item.Size;
		}

		ObjectInfo AddObj(string name, long size)
		{
			ObjectInfo obj = new ObjectInfo() { Name = name, Size = size };
			m_objects.Add(name, obj);
			return obj;
		}

		public ObjectInfo AtomCreate(string name, byte[] data, int off, int size)
		{
			FileTreeItem parent = m_loc.GetParent(m_root, name);
			parent.AtomCreateChild(name, data, off, size);
			return AddObj(name, size);
		}

		public ObjectInfo AtomCreate(string name, Stream istream, int off, int size)
		{
			FileTreeItem parent = m_loc.GetParent(m_root, name);
			parent.AtomCreateChild(name, istream, off, size);
			return AddObj(name, size);
		}

		public ObjectInfo AtomCreate(string name, byte[] data)
		{
			FileTreeItem parent = m_loc.GetParent(m_root, name);
			FileTreeItem item = parent.AtomCreateChild(name, data);
			return AddObj(name, item.Size);
		}

		public ObjectInfo AtomCreate(string name, Stream istream)
		{
			FileTreeItem parent = m_loc.GetParent(m_root, name);
			FileTreeItem item = parent.AtomCreateChild(name, istream);
			return AddObj(name, item.Size);
		}

		public void WalkData(string name, StorageKit.AcceptDataDelegate f)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			item.WalkData(f);
		}

		public byte[] ComputeHash(string name, string algorithm)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			return item.ComputeHash(algorithm);
		}

		#endregion

		#region Stream methods

		public Tuple<ObjectInfo, Stream> CreateObject(string name, long size)
		{
			FileTreeItem parent = m_loc.GetParent(m_root, name);
			var r = parent.CreateChild(name, size);
			ObjectInfo info = AddObj(name, size);
			return new Tuple<ObjectInfo, Stream>(info, r.Item2);
		}

		public Stream OpenObject(string name, bool writable)
		{
			FileTreeItem item = m_loc.GetObject(m_root, name);
			return item.Open(writable);
		}

		public ObjectInfo Refresh(string name)
		{
			ObjectInfo obj = m_objects[name];
			FileTreeItem fi = m_loc.GetObject(m_root, name);
			fi.Refresh();
			obj.Size = fi.Size;
			return obj;
		}

		#endregion

        public void Close()
        {
            m_objects = null;
            m_loc = null;
            m_root = null;
            m_acc.Close();
            m_acc = null;
        }
	}

	public interface SingleLayerKeyValueDivider
	{
		List<string> ListBuckets();
		string GetBucket(string key);
	}

	public class SingleLayerKeyValueLocator : SimpleKeyValueLocator
	{
		SingleLayerKeyValueDivider m_divider;

		public SingleLayerKeyValueLocator(SingleLayerKeyValueDivider divider)
		{
			m_divider = divider;
		}

		public void InitBuckets(FileTreeItem root)
		{
			foreach (string name in m_divider.ListBuckets())
				root.CreateDir(name);
		}

		public IEnumerable<ObjectInfo> List(FileTreeItem root)
		{
			foreach (string name in m_divider.ListBuckets())
			{
				var fiDir = root[name];
				foreach (FileTreeItem fi in fiDir.List())
				{
					Debug.Assert(!fi.IsDir);
					yield return new ObjectInfo() { Name = fi.Name, Size = fi.Size };
				}
			}
		}

		public void DeleteObjects(FileTreeItem root)
		{
			root.DeleteChildren();
			InitBuckets(root);
		}

		public FileTreeItem GetParent(FileTreeItem root, string name)
		{
			string buck = m_divider.GetBucket(name);
			return root[buck];
		}

		public FileTreeItem GetObject(FileTreeItem root, string name)
		{
			FileTreeItem parent = GetParent(root, name);
			return parent[name];
		}
	}
}
