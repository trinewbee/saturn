// deprecated

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Storage.Backward;
using Nano.Storage.Backward.Bfs;
using Nano.UnitTest;

namespace Nano.Obsolete.Tests
{
	class TestHybridBlockStorage
	{
		const string TestHybridBlockStoragePath = @"D:\Temp\TestHybridBlockStorage";

		static HybridBlockStorageConfig ms_config = null;

		static void TestCreateStorage(TestEnv e)
		{
			if (Directory.Exists(TestHybridBlockStoragePath))
				Directory.Delete(TestHybridBlockStoragePath, true);

			HybridBlockStorage stor = new HybridBlockStorage();
			stor.Open(TestHybridBlockStoragePath, ms_config, true);

			e.KVSCreateBlockWrite(stor, 0x400000 - 1, 1);
			e.KVSCreateBlockWrite(stor, 0x400000, 2);
			e.KVSCreateBlockWrite(stor, 0x400000 + 1, 3);

			stor.Close();
		}

		static void TestReadBlocks(TestEnv e)
		{
			HybridBlockStorage stor = new HybridBlockStorage();
			stor.Open(TestHybridBlockStoragePath, ms_config, false);

			e.KVSTestBlockRead(stor, 0x400000 - 1, 1);
			e.KVSTestBlockRead(stor, 0x400000, 2);
			e.KVSTestBlockRead(stor, 0x400000 + 1, 3);

			stor.Close();
		}

		public static void Run(TestEnv e)
		{
			ms_config = new HybridBlockStorageConfig();
			ms_config.bsc = BlockStorageConfig.Make64M();
			ms_config.diffSize = 0x400000;
			ms_config.kloc = new TailKeyLocator();

			Console.WriteLine("TestHybridBlockStorage");
			TestCreateStorage(e);
			TestReadBlocks(e);
		}
	}
}
