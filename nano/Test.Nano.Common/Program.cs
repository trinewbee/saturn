using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TestCommon
{
	static class Program
	{
		static void Main(string[] args)
		{
			// Test Stavl.Common
			TestJson.TestJsonBasic.Run();
			TestJson.TestJsonParse.Run();
			TestJson.TestJsonWriter.Run();
            TestJson.Test_JsonBasic.Run();
            TestJson.Test_JsonBuild.Run();
            TestJson.Test_JsonParse.Run();
            TestJson.Test_JsonFormat.Run();

            // Test Stavl.Collection
            TestCollection.TestCollectionKit.Run();
			TestCollection.TestListTable.Run();
			TestCollection.TestLRUCachePool.Run();
			TestCollection.TestRingBuffer.Run();

			// Test Stavl.Xml
			TestXml.Run();

			// Test Stavl.Storage
			TestStorage.TestLocalFileTreeAccess.Run();
			TestStorage.TestSimpleKeyValueAccess.Run();
			TestStorage.TestSimpleBlock.Run();
			TestStorage.FileTreeUpdateWalkerTest.Run();

            Nano.UnitTest.Test.Report();
		}
	}
}
