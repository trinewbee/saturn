using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Common;
using Nano.Storage;
using Nano.Storage.Common;
using Nano.UnitTest;

namespace TestCommon.TestStorage
{
	class TestLocalFileTreeAccess
	{
		const string pathTest = "testLocalFileTreeAccess";
		LocalFileTreeAccess m_access = null;
		FileTreeItem m_root = null;
		Comparison<FileTreeItem> cmp = (FileTreeItem lhs, FileTreeItem rhs) => string.Compare(lhs.Name, rhs.Name, true);

		TestLocalFileTreeAccess()
		{
			Test.InitDirectory(pathTest);

			m_access = new LocalFileTreeAccess(pathTest);
			m_root = m_access.Root;
			Test.Assert(m_root != null);
		}

		void Dispose()
		{
			m_access.Close();
			Directory.Delete(pathTest, true);
		}

		void testAtomCreate()
		{
			FileTreeItem fi = m_root.CreateDir("testAtomCreate");
			byte[] sa = Test.MakeData(1000, 6);
			MemoryStream ss = new MemoryStream(sa);

			FileTreeItem fi1 = fi.AtomCreateChild("1", sa);
			Test.Assert(fi1.Size == 1000);
			Test.AssertVerifyData(fi1.AtomRead(), 0, 1000, 6);
			Test.AssertVerifyData(fi1.AtomRead(250, 500), 0, 500, 0);

			FileTreeItem fi2 = fi.AtomCreateChild("2", sa, 250, 500);
			Test.Assert(fi2.Size == 500);
			Test.AssertVerifyData(fi2.AtomRead(), 0, 500, 0);

			FileTreeItem fi3 = fi.AtomCreateChild("3", ss);
			Test.Assert(fi3.Size == 1000);
			Test.AssertVerifyData(fi3.AtomRead(), 0, 1000, 6);

			FileTreeItem fi4 = fi.AtomCreateChild("4", ss, 250, 500);
			Test.Assert(fi4.Size == 500);
			Test.AssertVerifyData(fi4.AtomRead(), 0, 500, 0);
		}

		void testAtomWrite()
		{
			FileTreeItem fi = m_root.CreateDir("testAtomWrite");
			byte[] sa = Test.MakeData(1000, 8);
			MemoryStream ss = new MemoryStream(sa);

			FileTreeItem fi1 = fi.AtomCreateChild("1", sa, 0, 500);
			Test.Assert(fi1.Size == 500);
			fi1.AtomWrite(250, sa, 500, 500);
			Test.Assert(fi1.Size == 750);
			byte[] st = fi1.AtomRead();
			Test.AssertVerifyData(st, 0, 250, 8);
			Test.AssertVerifyData(st, 250, 500, (500 + 8) % 256);

			fi1.AtomWrite(500, ss);
			Test.Assert(fi1.Size == 1500);
			st = fi1.AtomRead();
			Test.AssertVerifyData(st, 0, 250, 8);
			Test.AssertVerifyData(st, 250, 250, (500 + 8) % 256);
			Test.AssertVerifyData(st, 500, 1000, 8);
		}

		void testWalkData()
		{
			byte[] data = Encoding.UTF8.GetBytes("Hello, world.");
			FileTreeItem fi = m_root.CreateDir("testWalkData");

			FileTreeItem fi1 = fi.AtomCreateChild("1", data);
			byte[] hash = fi1.ComputeHash("md5");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "080aef839b95facf73ec599375e92d47");

			hash = fi1.ComputeHash("sha1");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "2ae01472317d1935a84797ec1983ae243fc6aa28");

			FileTreeItem fi2 = fi.AtomCreateChild("2", new byte[0]);
			hash = fi2.ComputeHash("md5");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "d41d8cd98f00b204e9800998ecf8427e");
			hash = fi2.ComputeHash("sha1");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "da39a3ee5e6b4b0d3255bfef95601890afd80709");
		}

		void testStream()
		{
			FileTreeItem fi = m_root.CreateDir("testStream");
			byte[] sa = Test.MakeData(1000, 2);

			var r = fi.CreateChild("1", 250);
			FileTreeItem fi1 = r.Item1;
			Stream ostream = r.Item2;
			ostream.Write(sa, 0, 500);
			ostream.Seek(250, SeekOrigin.Begin);
			ostream.Write(sa, 500, 500);
			Test.Assert(ostream.Length == 750);			
			ostream.Close();

			// FileTreeItem not updated when operating a stream.
			// And even Refresh does not get the proper value until the stream closed.
			fi1.Refresh();
			Test.Assert(fi1.Size == 750);

			Stream istream = fi1.Open(false);
			byte[] buffer = new byte[1000];
			istream.Seek(250, SeekOrigin.Begin);
			Test.Assert(istream.Read(buffer, 0, 1000) == 500);
			Test.AssertVerifyData(buffer, 0, 500, (500 + 2) % 256);
			istream.Close();

			ostream = fi1.Open(true);
			ostream.Write(sa, 0, 1000);
			Test.Assert(ostream.Length == 1000);
			Test.AssertVerifyData(ostream, 1000, 2);
			ostream.Close();
		}

		void testDir()
		{
			FileTreeItem fi = m_root.CreateDir("testDir");
			FileTreeItem fi1 = fi.CreateDir("1");
			FileTreeItem fi2 = fi.AtomCreateChild("2", new byte[] { 1, 2, 3});
			FileTreeItem fi1a = fi1.AtomCreateChild("1a", new byte[] { 2, 3 });
			FileTreeItem fi1b = fi1.AtomCreateChild("1b", new byte[] { 4, 5 });

			List<FileTreeItem> fis = fi.List();
			fis.Sort(cmp);
			Test.Assert(fis.Count == 2 && fis[0].Name == "1" && fis[1].Name == "2");

			(fis = fi1.List()).Sort(cmp);
			Test.Assert(fis.Count == 2 && fis[0].Name == "1a" && fis[1].Name == "1b");
			Test.Assert(fi1["1a"].Name == "1a" && fi1["1b"].Name == "1b");

			fi1a.Delete(false);
			(fis = fi1.List()).Sort(cmp);
			Test.Assert(fis.Count == 1 && fis[0].Name == "1b");
			Test.Assert(fi1["1a"] == null && fi1["1b"].Name == "1b");

			fi1.Delete(true);
			(fis = fi.List()).Sort(cmp);
			Test.Assert(fis.Count == 1 && fis[0].Name == "2");
		}

		void testMove()
		{
			FileTreeItem fi = m_root.CreateDir("testMove");
			FileTreeItem fi1 = fi.CreateDir("1");
			FileTreeItem fi2 = fi.CreateDir("2");
			FileTreeItem fi1a = fi1.AtomCreateChild("1a", new byte[] { 1, 2 });
			FileTreeItem fi1b = fi1.AtomCreateChild("1b", new byte[] { 2, 3 });

			// rename file
			fi1a.MoveTo(fi1, "1c");
			Test.Assert(fi1a.Name == "1c" && fi1a.Parent.Name == "1");
			Test.Assert(fi1["1a"] == null && fi1["1c"].Name == "1c");

			// move file and rename
			fi1a.MoveTo(fi2, "1d");
			Test.Assert(fi1a.Name == "1d" && fi1a.Parent.Name == "2");
			var ls = fi1.List();
			Test.Assert(ls.Count == 1 && ls[0].Name == "1b");
			ls = fi2.List(); 
			Test.Assert(ls.Count == 1 && ls[0].Name == "1d");
			
			// move directory
			fi1.MoveTo(fi2, fi1.Name);
			Test.Assert(fi1.Name == "1" && fi1.Parent.Name == "2");
			ls = fi1.List();
			Test.Assert(ls.Count == 1 && ls[0].Name == "1b");
			(ls = fi2.List()).Sort(cmp);
			Test.Assert(ls.Count == 2 && ls[0].Name == "1" && ls[1].Name == "1d");

			// delete children
			fi2.DeleteChildren();
			Test.Assert(fi2.List().Count == 0);
		}

		public static void Run()
		{
			Console.WriteLine("TestStorage.TestLocalFileTreeAccess");
			TestLocalFileTreeAccess o = new TestLocalFileTreeAccess();
			o.testAtomCreate();
			o.testAtomWrite();
			o.testWalkData();
			o.testStream();
			o.testDir();
			o.testMove();
			o.Dispose();
		}
	}
}
