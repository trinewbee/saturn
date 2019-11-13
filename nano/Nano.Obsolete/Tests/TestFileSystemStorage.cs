// deprecated

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Storage.Backward;
using Nano.UnitTest;

namespace Nano.Obsolete.Tests
{
	class TestFileSystemStorage
	{
		const string TestPath = @"D:\Temp\TestFileSystemStorage";

		static void Assert(bool f)
		{
			if (!f)
				Console.WriteLine("Assertion failed");
		}

		static void TestCreateEnv()
		{
			if (Directory.Exists(TestPath))
				Directory.Delete(TestPath, true);

			Directory.CreateDirectory(TestPath);
			FileSystemStorageLibrary lib = new FileSystemStorageLibrary(TestPath);
			lib.Open();

			IStorageVersion ver = lib.Storage;
			Assert(ver.Library == lib);
			Assert((ver.TimestampUtc - DateTime.UtcNow).TotalSeconds < 1);

			IStorageFolder root = ver.Root;
			Assert(root.FileCount == 0 && root.SubFolderCount == 0);
			IStorageFolder d1 = root.CreateFolder("D1", false);
			IStorageFolder d2 = root.CreateFolder("D2", false);
			IStorageFolder d3 = root.CreateFolder("D3", false);

			IStorageFile f1;
			Stream fs = root.CreateFile("F1", out f1);
			fs.SetLength(80);
			fs.Close();
			 
			IStorageFile f2;
			fs = root.CreateFile("F2", out f2);
			fs.SetLength(4399);
			fs.Close();

			Assert(root.FileCount == 2 && root.SubFolderCount == 3);
			lib.Close();
		}

		static void TestReadEnv()
		{
			FileSystemStorageLibrary lib = new FileSystemStorageLibrary(TestPath);
			lib.Open();

			IStorageVersion ver = lib.Storage;
			IStorageFolder root = ver.Root;

			Assert(root.FileCount == 2 && root.SubFolderCount == 3);
			List<string> names = new List<string>();
			foreach (IStorageFolder folder in root.SubFolders)
				names.Add(folder.Name);
			names.Sort();
			Assert(names.Count == 3 && names[0] == "D1" && names[2] == "D3");

			names.Clear();
			foreach (IStorageFile file in root.Files)
				names.Add(file.Name);
			names.Sort();
			Assert(names.Count == 2 && names[0] == "F1" && names[1] == "F2");

			lib.Close();
		}

		public static void Run()
		{
			Console.WriteLine("TestFileSystemStorage");
			TestCreateEnv();
			TestReadEnv();
		}
	}
}
