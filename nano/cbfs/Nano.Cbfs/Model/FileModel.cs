using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CallbackFS;

namespace Nano.Cbfs.Model
{
	public interface IFileItem
	{
		object GetId();
		IFileAlloc GetAlloc();
		string GetName();
		IFileItem GetParent();
		bool IsDir();
		bool IsEmpty();
		IEnumerable<IFileItem> List();
		IFileItem GetChild(string name);
		WdFileInfo ToFileInfo();
	}

	public interface IFileModel
	{
		uint GetVolumeId();
		string GetLabel();
		void SetLabel(string label);
		IFileItem GetRoot();
		IFileItem GetItem(object id);
		IFileItem FindFile(string path);
		IFileItem FindParentDirectory(string path, out string name);
		IFileItem CreateFile(IFileItem parent, string name, uint attrs, IFileAlloc alloc);
		void UpdateAttrs(IFileItem item, uint attrs);
		void UpdateTime(IFileItem item, DateTime ctime, DateTime mtime, DateTime atime);
		void RemoveFile(IFileItem item);
		void MoveFile(IFileItem item, IFileItem parent, string name);
		void Close();
	}

	class SimpleFileItem : IFileItem
	{
		internal SimpleFileItem m_parent;
		internal string m_name;
		internal uint m_attrs;
		internal DateTime m_ctime, m_mtime, m_atime;
		internal IFileAlloc m_alloc;
		internal SortedDictionary<string, SimpleFileItem> m_children;

		internal SimpleFileItem(SimpleFileItem parent, string name, uint attrs, IFileAlloc alloc)
		{
			m_parent = parent;
			m_name = name;
			m_attrs = attrs;
			m_ctime = m_mtime = m_atime = DateTime.UtcNow;
			m_alloc = alloc;
			if (!WdFileInfo.IsAttrFile(attrs))
				m_children = new SortedDictionary<string, SimpleFileItem>(StringComparer.CurrentCulture);
			else
				m_children = null;
		}

		public object GetId() => null;

		public IFileAlloc GetAlloc() => m_alloc;

		public string GetName() => m_name;

		public IFileItem GetParent() => m_parent;

		public bool IsDir() => !WdFileInfo.IsAttrFile(m_attrs);

		public bool IsEmpty() => m_children == null || m_children.Count == 0;

		public IEnumerable<IFileItem> List() => m_children.Values;

		public IFileItem GetChild(string name)
		{
			SimpleFileItem item;
			if (m_children.TryGetValue(name.ToLower(), out item))
				return item;
			return null;
		}

		internal void _AddChild(SimpleFileItem child) => m_children.Add(child.m_name.ToLower(), child);

		internal void _RemoveChild(SimpleFileItem child)
		{
			if (!m_children.Remove(child.m_name.ToLower()))
				throw new ECBFSError(ErrCode.ERROR_FILE_NOT_FOUND);
		}

		public WdFileInfo ToFileInfo()
		{
			var info = new WdFileInfo();
			info.Name = m_name;
			info.Attr = m_attrs;
			info.CTime = m_ctime;
			info.MTime = m_mtime;
			info.ATime = m_atime;
			info.FileSize = m_alloc != null ? m_alloc.GetLength() : 0;
			info.AllocSize = m_alloc != null ? m_alloc.GetAllocation() : 0;
			return info;
		}
	}

	public class SimpleFileModel : IFileModel
	{
		string m_label;
		uint m_volumeId;
		SimpleFileItem m_root;

		public SimpleFileModel()
		{
			m_label = "Test Drive";
			m_volumeId = 0x20110928;
			m_root = new SimpleFileItem(null, null, WdFileInfo.FILE_ATTRIBUTE_DEVICE | WdFileInfo.FILE_ATTRIBUTE_DIRECTORY, null);
		}

		public uint GetVolumeId() => m_volumeId;

		public string GetLabel() => m_label;

		public void SetLabel(string label) => m_label = label;

		public IFileItem GetRoot() => m_root;

		public IFileItem GetItem(object id) { throw new NotImplementedException(); }

		public IFileItem FindFile(string path) => FindFile(m_root, path);

		public static IFileItem FindFile(IFileItem item, string path)
		{
			Debug.Assert(path[0] == '\\');
			int pos = 1, len = path.Length;
			while (pos < len)
			{
				int pos2 = path.IndexOf('\\', pos);
				if (pos2 < 0)
					pos2 = len;
				string name = path.Substring(pos, pos2 - pos);
				item = item.GetChild(name);
				if (item == null)
					return null;
				pos = pos2 < len ? pos2 + 1 : len;
			}
			return item;
		}

		public IFileItem FindParentDirectory(string path, out string name) => FindParentDirectory(m_root, path, out name);

		public static IFileItem FindParentDirectory(IFileItem item, string path, out string name)
		{
			Debug.Assert(path[0] == '\\' && path[path.Length - 1] != '\\');
			int pos = path.LastIndexOf('\\');
			Debug.Assert(pos >= 2 || pos == 0);
			name = path.Substring(pos + 1);
			if (pos > 0)
				return FindFile(item, path.Substring(0, pos));
			else
				return item;
		}

		public IFileItem CreateFile(IFileItem parent, string name, uint attrs, IFileAlloc alloc)
		{
			var sparent = (SimpleFileItem)parent;
			var fi = new SimpleFileItem(sparent, name, attrs, alloc);
			sparent._AddChild(fi);
			return fi;
		}

		public void UpdateAttrs(IFileItem item, uint attrs) => ((SimpleFileItem)item).m_attrs = attrs;

		public void UpdateTime(IFileItem item, DateTime ctime, DateTime mtime, DateTime atime)
		{
			var sitem = (SimpleFileItem)item;
			if (ctime != DateTime.MinValue)
				sitem.m_ctime = ctime;
			if (mtime != DateTime.MinValue)
				sitem.m_mtime = mtime;
			if (atime != DateTime.MinValue)
				sitem.m_atime = atime;
		}

		public void RemoveFile(IFileItem item)
		{
			var sitem = (SimpleFileItem)item;
			var parent = sitem.m_parent;
			Debug.Assert(parent != null);
			parent._RemoveChild(sitem);
		}

		public void MoveFile(IFileItem item, IFileItem parent, string name)
		{
			var sitem = (SimpleFileItem)item;
			var sparent = (SimpleFileItem)parent;
			sitem.m_parent._RemoveChild(sitem);
			sitem.m_parent = sparent;
			sitem.m_name = name;
			sparent._AddChild(sitem);
		}

		public void Close() { }
	}
}
