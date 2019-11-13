using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestExt
{
	class Program
	{
		static void Main(string[] args)
		{
            // Code Model
            TestLEM.Run();

            // Crypt
            TestBlockCrypt.Run();
			TestCryptKeyValueAccess.Run();
			TestCryptFileTreeAccess.Run();

			// Marshal
			TestJsonModel.Run();
			TestJsonMarshal.Run();
			// TestDynJson.Run(); // replaced by DObject
			TestOdlParser.Run();

			// Persist
#if true
			TestBinlog.Run();
			TestBom.Run();
#endif

			// Nuts
			TestNuts.TestObject.Run();

            Nano.UnitTest.Test.Report();
		}
	}
}
