using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Common;
using Nano.Storage.Backward;
using Nano.UnitTest;

namespace Nano.Obsolete.Tests
{
	class TestEnv
	{
		#region Test key-value object storage

		public static BinaryValue MakeUid(int value)
		{
			byte[] uid = new byte[24];
			ExtConvert.CopyToArray(uid, 0, value);
			return new BinaryValue(uid);
		}

		public void KVSCreateBlock(IKeyValueStorage bfs, int size)
		{
			Stream stream = bfs.CreateFile(MakeUid(size), size);
			stream.Close();
		}

		public void KVSCreateBlockWrite(IKeyValueStorage bfs, int size, byte seed)
		{
			Stream stream = bfs.CreateFile(MakeUid(size), size);
			Test.WriteData(stream, size, seed);
			stream.Close();
		}

		public void KVSTestBlock(IKeyValueStorage bfs, int size)
		{
			Stream stream = bfs.OpenFile(MakeUid(size), DataOpenMode.Read);
			Test.Assert(stream.Length == size);
			stream.Close();
		}

		public void KVSTestBlockRead(IKeyValueStorage bfs, int size, byte seed)
		{
			Stream stream = bfs.OpenFile(MakeUid(size), DataOpenMode.Read);
			Test.AssertVerifyData(stream, size, seed);
			stream.Close();
		}

		#endregion
	}
}
