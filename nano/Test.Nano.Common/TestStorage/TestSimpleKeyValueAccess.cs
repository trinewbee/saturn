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
	class TestSimpleKeyValueAccess
	{
		const string pathTest = "testSimpleKeyValueAccess";
		SingleLayerKeyValueLocator m_loc;
		LocalFileTreeAccess m_fta;
		SimpleKeyValueAccess m_access;
		Comparison<ObjectInfo> cmp = (lhs, rhs) => string.Compare(lhs.Name, rhs.Name, true);

		class TestDivider : SingleLayerKeyValueDivider
		{
			const int N = 16;

			public string GetBucket(string key) => ((key.GetHashCode() & 0x7FFFFFFF) % N).ToString();

			public List<string> ListBuckets()
			{
				var bucks = new List<string>();
				for (int i = 0; i < N; ++i)
					bucks.Add(i.ToString());
				return bucks;
			}
		}

		TestSimpleKeyValueAccess()
		{
			Test.InitDirectory(pathTest);

			m_loc = new SingleLayerKeyValueLocator(new TestDivider());
			m_fta = new LocalFileTreeAccess(pathTest);
			m_loc.InitBuckets(m_fta.Root);
			m_access = new SimpleKeyValueAccess(m_fta, m_fta.Root, m_loc);
		}

		void Dispose()
		{
			Directory.Delete(pathTest, true);
		}

		void testAtomCreate()
		{
			byte[] sa = Test.MakeData(1000, 6);
			MemoryStream ss = new MemoryStream(sa);

			ObjectInfo info = m_access.AtomCreate("1", sa);
			Test.Assert(info.Size == 1000);
			Test.AssertVerifyData(m_access.AtomRead("1"), 0, 1000, 6);
			Test.AssertVerifyData(m_access.AtomRead("1", 250, 500), 0, 500, 0);

			info = m_access.AtomCreate("2", sa, 250, 500);
			Test.Assert(info.Size == 500);
			Test.AssertVerifyData(m_access.AtomRead("2"), 0, 500, 0);

			info = m_access.AtomCreate("3", ss);
			Test.Assert(info.Size == 1000);
			Test.AssertVerifyData(m_access.AtomRead("3"), 0, 1000, 6);

			info = m_access.AtomCreate("4", ss, 250, 500);
			Test.Assert(info.Size == 500);
			Test.AssertVerifyData(m_access.AtomRead("4"), 0, 500, 0);
		}

		void testAtomWrite()
		{
			byte[] sa = Test.MakeData(1000, 8);
			MemoryStream ss = new MemoryStream(sa);

			string name = "w1";
			ObjectInfo info = m_access.AtomCreate(name, sa, 0, 500);
			m_access.AtomWrite(name, 250, sa, 500, 500);
			Test.Assert(info.Size == 750);
			byte[] st = m_access.AtomRead(name);
			Test.AssertVerifyData(st, 0, 250, 8);
			Test.AssertVerifyData(st, 250, 500, (500 + 8) % 256);

			m_access.AtomWrite(name, 500, ss);
			Test.Assert(info.Size == 1500);
			st = m_access.AtomRead(name);
			Test.AssertVerifyData(st, 0, 250, 8);
			Test.AssertVerifyData(st, 250, 250, (500 + 8) % 256);
			Test.AssertVerifyData(st, 500, 1000, 8);
		}

		void testWalkData()
		{
			byte[] data = Encoding.UTF8.GetBytes("Hello, world.");

			string name = "h1";
			m_access.AtomCreate(name, data);
			byte[] hash = m_access.ComputeHash(name, "md5");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "080aef839b95facf73ec599375e92d47");

			hash = m_access.ComputeHash(name, "sha1");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "2ae01472317d1935a84797ec1983ae243fc6aa28");

			name = "h2";
			m_access.AtomCreate(name, new byte[0]);
			hash = m_access.ComputeHash(name, "md5");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "d41d8cd98f00b204e9800998ecf8427e");
			hash = m_access.ComputeHash(name, "sha1");
			Test.Assert(BinaryValue.ToHexString(hash).ToLowerInvariant() == "da39a3ee5e6b4b0d3255bfef95601890afd80709");
		}

		void testStream()
		{
			byte[] sa = Test.MakeData(1000, 2);
			string name = "s1";

			var r = m_access.CreateObject(name, 250);
			ObjectInfo info = r.Item1;
			Stream ostream = r.Item2;
			Test.Assert(info.Size == 250);

			ostream.Write(sa, 0, 500);
			ostream.Seek(250, SeekOrigin.Begin);
			ostream.Write(sa, 500, 500);
			Test.Assert(ostream.Length == 750);
			ostream.Close();

			// ObjectInfo not updated when operating a stream.
			// And even Refresh does not get the proper value until the stream closed.
			Test.Assert(m_access.Refresh(name).Size == 750);

			Stream istream = m_access.OpenObject(name, false);
			byte[] buffer = new byte[1000];
			istream.Seek(250, SeekOrigin.Begin);
			Test.Assert(istream.Read(buffer, 0, 1000) == 500);
			Test.AssertVerifyData(buffer, 0, 500, (500 + 2) % 256);
			istream.Close();

			ostream = m_access.OpenObject(name, true);
			ostream.Write(sa, 0, 1000);
			Test.Assert(ostream.Length == 1000);
			Test.AssertVerifyData(ostream, 1000, 2);
			ostream.Close();
		}

		void testDir()
		{
			byte[] sa = Test.MakeData(8, 0);
			Stream ss = new MemoryStream(sa);

			m_access.DeleteAll();
			Test.Assert(m_access.ListObjects().Count == 0);

			m_access.AtomCreate("d1", sa);
			m_access.AtomCreate("d2", sa, 2, 4);
			m_access.AtomCreate("d3", ss);
			m_access.AtomCreate("d4", ss, 3, 3);

			var ostream = m_access.CreateObject("d5", 0).Item2;
			ostream.Write(sa, 0, 5);
			ostream.Close();			

			List<ObjectInfo> objs = m_access.ListObjects();
			objs.Sort(cmp);
			Test.Assert(objs.Count == 5);
			Test.Assert(objs[0].Name == "d1" && objs[0].Size == 8);
			Test.Assert(objs[1].Name == "d2" && objs[1].Size == 4);
			Test.Assert(objs[2].Name == "d3" && objs[2].Size == 8);
			Test.Assert(objs[3].Name == "d4" && objs[3].Size == 3);
			Test.Assert(objs[4].Name == "d5" && objs[4].Size == 0);	// not updated
			Test.Assert(m_access.Refresh("d5").Size == 5);	// update object info

			m_access.DeleteObject("d3");
			objs = m_access.ListObjects();
			Test.Assert(objs.Count == 4 && m_access["d3"] == null);
		}

		public static void Run()
		{
			Console.WriteLine("TestStorage.TestSimpleKeyValueAccess");
			TestSimpleKeyValueAccess o = new TestSimpleKeyValueAccess();
			o.testAtomCreate();
			o.testAtomWrite();
			o.testWalkData();
			o.testStream();
			o.testDir();
			o.Dispose();
		}
	}
}
