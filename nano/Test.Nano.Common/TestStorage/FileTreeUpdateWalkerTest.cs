using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Common;
using Nano.Collection;
using Nano.Storage;
using Nano.UnitTest;

namespace TestCommon.TestStorage
{
	class _FTU_TestItem : FileTreeItem
	{
		public static DateTime TimeSeed = new DateTime(2011, 9, 28, 0, 0, 0, DateTimeKind.Utc);

		public static DateTime G_Time() => TimeSeed = TimeSeed.AddTicks(10000000);

		_FTU_TestItem m_parent;

		public string Name { get; set; }
		public bool IsDir { get; set; }
		public DateTime LastWriteTimeUtc { get; set; }
		public long Size { get; set; }

		SortedDictionary<string, _FTU_TestItem> m_children;

		public FileTreeItem Parent => m_parent;

		public _FTU_TestItem(_FTU_TestItem parent, string name, bool isdir, DateTime mtime, long size = 0)
		{
			m_parent = parent;
			Name = name;
			IsDir = isdir;
			LastWriteTimeUtc = mtime;
			Size = size;
			m_children = isdir ? new SortedDictionary<string, _FTU_TestItem>() : null;
		}

		public FileTreeItem this[string name] => m_children[name.ToLowerInvariant()];

		public FileTreeItem AtomCreateChild(string name, byte[] data, int off, int size) => throw new NotImplementedException();

		public FileTreeItem AtomCreateChild(string name, Stream istream, long off, long size) => throw new NotImplementedException();

		public FileTreeItem AtomCreateChild(string name, byte[] data)
		{
			var item = new _FTU_TestItem(this, name, false, G_Time(), data.Length);
			m_children.Add(name.ToLowerInvariant(), item);
			return item;
		}

		public FileTreeItem AtomCreateChild(string name, Stream istream) => throw new NotImplementedException();

		public byte[] AtomRead(long pos, int size) => throw new NotImplementedException();

		public byte[] AtomRead() => new byte[Size];

		public void AtomWrite(long pos, byte[] data, int off, int size) => throw new NotImplementedException();

		public void AtomWrite(long pos, Stream istream, long off, long size) => throw new NotImplementedException();

		public void AtomWrite(long pos, Stream istream) => throw new NotImplementedException();

		public byte[] ComputeHash(string algorithm) => throw new NotImplementedException();

		public void WalkData(StorageKit.AcceptDataDelegate f) => throw new NotImplementedException();

		public Stream Open(bool writable) => throw new NotImplementedException();

		public Tuple<FileTreeItem, Stream> CreateChild(string name, long size) => throw new NotImplementedException();

		public FileTreeItem CreateDir(string name)
		{
			var item = new _FTU_TestItem(this, name, true, G_Time(), 0);
			m_children.Add(name.ToLowerInvariant(), item);
			return item;
		}

		public void Delete(bool recursive) => m_parent.m_children.Remove(Name.ToLowerInvariant());

		public void DeleteChildren() => m_children.Clear();

		public List<FileTreeItem> List() => CollectionKit.ToList<FileTreeItem>(m_children.Values);

		public void MoveTo(FileTreeItem parent, string name)
		{
			var parentOld = m_parent;
			var nameOld = Name;
			m_parent.m_children.Remove(nameOld);

			m_parent = (_FTU_TestItem)parent;
			Name = name;
			m_parent.m_children.Add(name.ToLowerInvariant(), this);
		}

		public void Refresh() { }

		public override string ToString() => IsDir ? '[' + Name + ']' : Name;

		public static bool CompareTree(_FTU_TestItem x, _FTU_TestItem y)
		{
			if (x.IsDir != y.IsDir || x.Name != y.Name || x.LastWriteTimeUtc != y.LastWriteTimeUtc)
				return false;

			if (x.IsDir)
			{
				var ls_x = x.List();
				var ls_y = y.List();
				if (ls_x.Count != ls_y.Count)
					return false;
				for (int i = 0; i < ls_x.Count; ++i)
				{
					var xi = (_FTU_TestItem)ls_x[i];
					var yi = (_FTU_TestItem)ls_y[i];
					Test.Assert(xi.Parent == x && yi.Parent == y);
					if (!CompareTree(xi, yi))
						return false;
				}
				return true;
			}
			else
				return x.Size == y.Size;
		}

	}

	class _FTU_TestAccess : FileTreeAccess
	{
		_FTU_TestItem m_root = new _FTU_TestItem(null, "", true, _FTU_TestItem.TimeSeed);

		public FileTreeItem Root => m_root;

		public bool CanCreate => true;

		public bool CanResize => false;

		public bool CanAppend => false;

		public bool CanModify => false;

		public bool SupportStream => false;

		public bool IsRemote => false;

		public long MaxSize => int.MaxValue;

		public string[] HashSaved => null;

		public void Close() { }
	}

	class _FTU_TestNotify : IFileTreeUpdateNotify
	{
		public List<string> Logs = null;

		public void Send(FileTreeItem source, FileTreeItem target, string pathRel, FileUpdateActionType act)
		{
			switch (act)
			{
				case FileUpdateActionType.CreateDirectory:
					Logs.Add("NCD:" + pathRel);
					break;
				case FileUpdateActionType.DeleteDirectory:
					Logs.Add("NDD:" + pathRel);
					break;
				case FileUpdateActionType.DeleteFile:
					Logs.Add("NDF:" + pathRel);
					break;
				case FileUpdateActionType.EnterDirectory:
					Logs.Add("NED:" + pathRel);
					break;
				case FileUpdateActionType.LeaveDirectory:
					Logs.Add("NLD:" + pathRel);
					break;
				case FileUpdateActionType.UpdateFile:
					Logs.Add("NUF:" + pathRel);
					break;
				default:
					throw new ArgumentException("Unsupported action code: " + act.ToString());
			}
		}
	}

	class _FTU_TestUpdate : IFileTreeUpdater
	{
		public List<string> Logs = null;

		public FileTreeItem Create(FileTreeItem source, FileTreeItem parent, string name, string pathRel)
		{
			Logs?.Add("DCF:" + pathRel);
			var data = source.AtomRead();
			return parent.AtomCreateChild(name, data);
		}

		public FileTreeItem Update(FileTreeItem source, FileTreeItem target, string pathRel)
		{
			Logs?.Add("DUF:" + pathRel);
			var parent = target.Parent;
			var name = target.Name;
			target.Delete(true);
			var data = source.AtomRead();
			return parent.AtomCreateChild(name, data);
		}
	}

	class FileTreeUpdateWalkerTest
	{
		_FTU_TestItem Source = (_FTU_TestItem)new _FTU_TestAccess().Root;
		_FTU_TestItem Target = (_FTU_TestItem)new _FTU_TestAccess().Root;
		List<string> Logs = new List<string>();

		void testSimple()
		{
			RunUpdate();
			Test.Assert(Target.List().Count == 0);
			Test.AssertListEqual(Logs, new string[] { "NED:/", "NLD:/" });

			Source.AtomCreateChild("a", new byte[1]);
			RunUpdate();
			var fis = Target.List();
			Test.Assert(fis.Count == 1 && isFile(fis[0], "a", 1));
			Test.AssertListEqual(Logs, new string[] { "NED:/", "NUF:/a", "DCF:/a", "NLD:/" });

			changeSize(Source["a"], 2);
			RunUpdate();
			fis = Target.List();
			Test.Assert(fis.Count == 1 && isFile(fis[0], "a", 2));
			Test.AssertListEqual(Logs, new string[] { "NED:/", "NUF:/a", "DUF:/a", "NLD:/" });

			Source["a"].Delete(false);
			RunUpdate();
			Test.Assert(Target.List().Count == 0);
			Test.AssertListEqual(Logs, new string[] { "NED:/", "NDF:/a", "NLD:/" });
		}

		void testFolder()
		{
			// 生成多个目录和文件
			var a = Source.CreateDir("a");
			a.CreateDir("ab");
			var aa = a.CreateDir("aa");
			aa.AtomCreateChild("aa2", new byte[2]);
			aa.AtomCreateChild("aa1", new byte[1]);

			RunUpdate();
			var fis = Target.List();
			Test.Assert(fis.Count == 1 && isDir(fis[0], "a"));
			fis = fis[0].List();
			Test.Assert(fis.Count == 2 && isDir(fis[0], "aa") && isDir(fis[1], "ab"));
			fis = fis[0].List();
			Test.Assert(fis.Count == 2 && isFile(fis[0], "aa1", 1) && isFile(fis[1], "aa2", 2));

			// 修改文件内容
			changeSize(a = Source["a"]["aa"]["aa1"], 3);
			RunUpdate();
			var b = Target["a"]["aa"]["aa1"];
			Test.Assert(a.LastWriteTimeUtc == b.LastWriteTimeUtc && b.Size == 3);

			// 修改文件名
			a = Source["a"]["aa"];
			a["aa1"].MoveTo(a, "aa3");
			RunUpdate();
			fis = Target["a"]["aa"].List();
			Test.Assert(fis.Count == 2 && isFile(fis[0], "aa2", 2) && isFile(fis[1], "aa3", 3));

			// 修改文件夹
			a = Source["a"];
			a["aa"].MoveTo(a, "ac");
			RunUpdate();
			fis = Target["a"].List();
			Test.Assert(fis.Count == 2 && isDir(fis[0], "ab") && isDir(fis[1], "ac"));

			// 删除文件和文件夹
			Source["a"]["ac"]["aa2"].Delete(false);
			Source["a"]["ab"].Delete(false);
			RunUpdate();
			fis = Target["a"].List();
			Test.Assert(fis.Count == 1 && isDir(fis[0], "ac"));
			fis = fis[0].List();
			Test.Assert(fis.Count == 1 && isFile(fis[0], "aa3", 3));

			Source["a"].Delete(true);
			RunUpdate();
			Test.Assert(Source.List().Count == 0);
		}

		void testNameConf()
		{
			// 文件转为文件夹
			var a = Source.CreateDir("a");
			a.AtomCreateChild("a1", new byte[1]);
			Target.AtomCreateChild("a", new byte[2]);
			RunUpdate();
			var fis = Target.List();
			Test.Assert(fis.Count == 1 && isDir(fis[0], "a"));
			fis = fis[0].List();
			Test.Assert(fis.Count == 1 && isFile(fis[0], "a1", 1));

			// 文件夹转为文件
			Source["a"].Delete(true);
			Source.AtomCreateChild("a", new byte[3]);
			RunUpdate();
			fis = Target.List();
			Test.Assert(fis.Count == 1 && isFile(fis[0], "a", 3));

			Source.DeleteChildren();
			Target.DeleteChildren();
			Test.Assert(_FTU_TestItem.CompareTree(Source, Target));
		}

		void RunUpdate()
		{
			Logs.Clear();
			var updater = new _FTU_TestUpdate { Logs = Logs };
			var notify = new _FTU_TestNotify { Logs = Logs };
			var walker = new FileTreeUpdateWalker(Source, Target, updater, notify: notify);
			walker.Walk();
			Test.Assert(_FTU_TestItem.CompareTree(Source, Target));
		}

		bool isFile(FileTreeItem fi, string name, long size) => !fi.IsDir && fi.Name == name && fi.Size == size;

		bool isDir(FileTreeItem fi, string name) => fi.IsDir && fi.Name == name;

		void changeSize(FileTreeItem fi, long size)
		{
			var efi = (_FTU_TestItem)fi;
			efi.Size = size;
			efi.LastWriteTimeUtc = _FTU_TestItem.G_Time();
		}

		public static void Run()
		{
			Console.WriteLine("TestStorage." + nameof(FileTreeUpdateWalkerTest));
			var o = new FileTreeUpdateWalkerTest();
			o.testSimple();
			o.testFolder();
			o.testNameConf();
		}
	}
}
