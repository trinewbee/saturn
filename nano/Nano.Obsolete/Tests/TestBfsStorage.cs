// deprecated

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Common;
using Nano.Storage.Backward;
using Nano.Storage.Backward.Bfs;
using Nano.UnitTest;

namespace Nano.Obsolete.Tests
{
	static class TestBlockStorage
	{
		const string TestBlockStoragePath = @"D:\Temp\TestBlockStorage";

		static BlockStorageConfig ms_bsconfig = BlockStorageConfig.Make64M();

		#region Test creating and reading blocks

		static void TestCreateBlocks(TestEnv e)
		{
			if (Directory.Exists(TestBlockStoragePath))
				Directory.Delete(TestBlockStoragePath, true);

			BlockStorage bfs = new BlockStorage();
			bfs.Open(TestBlockStoragePath, ms_bsconfig, true);

			e.KVSCreateBlockWrite(bfs, 1, 1);
			e.KVSCreateBlockWrite(bfs, 4095, 2);
			e.KVSCreateBlockWrite(bfs, 4096, 3);
			e.KVSCreateBlockWrite(bfs, 4097, 4);
			e.KVSCreateBlockWrite(bfs, 0x400000, 5);

			e.KVSCreateBlock(bfs, 0x100000 * 31);
			e.KVSCreateBlock(bfs, 0x100000 * 32);
			e.KVSCreateBlock(bfs, 0x100000 * 14);
			e.KVSCreateBlock(bfs, 0x100000 * 15);
			e.KVSCreateBlock(bfs, 0x100000 * 16);
			e.KVSCreateBlock(bfs, 0x100000 * 17);

			e.KVSCreateBlockWrite(bfs, 0x10000, 6);

			e.KVSTestBlockRead(bfs, 1, 1);
			e.KVSTestBlock(bfs, 0x100000 * 17);

			bfs.Close();
		}

		static void TestReadBlocks(TestEnv e)
		{
			BlockStorage bfs = new BlockStorage();
			bfs.Open(TestBlockStoragePath, ms_bsconfig);

			e.KVSTestBlockRead(bfs, 1, 1);
			e.KVSTestBlockRead(bfs, 4095, 2);
			e.KVSTestBlockRead(bfs, 4096, 3);
			e.KVSTestBlockRead(bfs, 4097, 4);
			e.KVSTestBlockRead(bfs, 0x400000, 5);

			e.KVSTestBlock(bfs, 0x100000 * 31);
			e.KVSTestBlock(bfs, 0x100000 * 32);
			e.KVSTestBlock(bfs, 0x100000 * 14);
			e.KVSTestBlock(bfs, 0x100000 * 15);
			e.KVSTestBlock(bfs, 0x100000 * 16);
			e.KVSTestBlock(bfs, 0x100000 * 17);

			e.KVSTestBlockRead(bfs, 0x10000, 6);

			bfs.Close();
		}

		#endregion

		public static void Run(TestEnv e)
		{
			Console.WriteLine("TestBfsStorage");
			TestCreateBlocks(e);
			TestReadBlocks(e);
		}
	}
}
