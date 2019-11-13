using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Storage;
using Nano.Storage.Common;
using Nano.UnitTest;

namespace TestCommon.TestStorage
{
	class TestSimpleBlock
	{
		const string pathTest = "testSimpleBlock";
		BfsConfig config;
		FileTreeAccess m_accs;
		FileTreeItem m_dir;

		TestSimpleBlock()
		{
			Test.InitDirectory(pathTest);
			config = new BfsConfig()
			{
				PageSize = 0x1000,		// 4KB
				TotalPages = 0x100,		// 1MB
				IndexPages = 4			// 16KB
			};
			m_accs = new LocalFileTreeAccess(pathTest);
			m_dir = m_accs.Root;
		}

		#region Test variant size block

		void testVariantBlockBasic()
		{			
			BfsVariantSizeBlock block = new BfsVariantSizeBlock();
			block.Create(m_dir, "test-vbs", config);
			Test.Assert(block.FreePageCount == 0xFC);
			_vsbWriteFile("1", 1, 1, block);
			Test.Assert(block.FreePageCount == 0xFB);
			_vsbWriteFile("4095", 2, 4095, block);
			Test.Assert(block.FreePageCount == 0xFA);
			_vsbWriteFile("4096", 3, 4096, block);
			Test.Assert(block.FreePageCount == 0xF9);
			_vsbWriteFile("4097", 4, 4097, block);
			Test.Assert(block.FreePageCount == 0xF7);

			string name5 = new string('a', 48);
			int size5 = 0x100000 - 0x4000 - 0x5000;
			_vsbWriteFile(name5, 5, size5, block);
			Test.Assert(block.FreePageCount == 0);

			int ientry;
			Test.Assert(block.CreateFile("e", 1, out ientry) == null);

			block.Close();

			block = new BfsVariantSizeBlock();
			block.Open(m_dir["test-vbs"], config);

			_vsbCheckFile("1", 1, 1, block);
			_vsbCheckFile("4095", 2, 4095, block);
			_vsbCheckFile("4096", 3, 4096, block);
			_vsbCheckFile("4097", 4, 4097, block);
			_vsbCheckFile(name5, 5, size5, block);

			BfsEntry entry = block.Entries[2];
			Test.Assert(entry.Name == "4095");
			byte[] data = new byte[4096];
			Test.Assert(block.ReadObject(entry.Index, 0, data, 0, 4096) == 4095);
			Test.AssertVerifyData(data, 0, 4095, 2);
			data.Initialize();
			Test.Assert(block.ReadObject(entry.Index, 4090, data, 2048, 2048) == 5);
			Test.AssertVerifyData(data, 2048, 5, 252);
			Test.Assert(block.ReadObject(entry.Index, 4095, data, 0, 1) == 0);
			Test.Assert(block.ReadObject(entry.Index, 4096, data, 0, 1) == 0);

			block.Close();

			m_dir["test-vbs"].Delete(false);
		}

		void testVariantBlockWriteObject()
		{
		}

		static void _vsbWriteFile(string name, byte seed, int size, BfsVariantSizeBlock block)
		{
			int ientry;
			var stream = block.CreateFile(name, size, out ientry);
			Test.WriteData(stream, size, seed);
			stream.Close();
		}

		static void _vsbCheckFile(string name, byte seed, int size, BfsVariantSizeBlock block)
		{
			var entry = block.Entries.Find(x => x != null && x.Name == name);
			Test.Assert(entry != null);
			var stream = block.OpenFile(entry.Index, false);
			Test.AssertVerifyData(stream, size, seed);
			stream.Close();
		}

		#endregion

		#region Test SO key value access

		void testSoKeyValueStream()
		{
			BfsSoKeyValueAccess accs = new BfsSoKeyValueAccess(config, m_accs, m_dir);
			_kvStreamWriteFile("1", 1, 1, accs);
			_kvStreamWriteFile("2", 2, 0x400 * 512, accs);
			_kvStreamWriteFile("3", 3, 0x400 * 512, accs);
			_kvStreamWriteFile("4", 3, 0x200 * 512, accs);
			_kvStreamWriteFile("5", 3, 0x100000 - 0x4000, accs);
			accs.Close();

			accs = new BfsSoKeyValueAccess(config, m_accs, m_dir);
			_kvStreamCheckFile("1", 1, 1, accs);
			_kvStreamCheckFile("2", 2, 0x400 * 512, accs);
			_kvStreamCheckFile("3", 3, 0x400 * 512, accs);
			_kvStreamCheckFile("4", 3, 0x200 * 512, accs);
			_kvStreamCheckFile("5", 3, 0x100000 - 0x4000, accs);
			accs.Close();

			m_dir.DeleteChildren();
		}

		static void _kvStreamWriteFile(string name, byte seed, int size, KeyValueAccess bucket)
		{
			var stream = bucket.CreateObject(name, size).Item2;
			Test.WriteData(stream, size, seed);
			stream.Close();
		}

		static void _kvStreamCheckFile(string name, byte seed, int size, KeyValueAccess bucket)
		{
			var stream = bucket.OpenObject(name, false);
			Test.AssertVerifyData(stream, size, seed);
			stream.Close();
		}

		void testSoKeyValueAtom()
		{
			BfsSoKeyValueAccess accs = new BfsSoKeyValueAccess(config, m_accs, m_dir);
			accs.Close();

			accs = new BfsSoKeyValueAccess(config, m_accs, m_dir);
			accs.Close();

			m_dir.DeleteChildren();
		}

		#endregion

		public static void Run()
		{
			Console.WriteLine("TestStorage.TestSimpleBlock");
			TestSimpleBlock o = new TestSimpleBlock();
			o.testVariantBlockBasic();
			o.testVariantBlockWriteObject();
			o.testSoKeyValueStream();
			o.testSoKeyValueAtom();
		}
	}
}
