using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Nano.Mysql
{
	static class Test
	{
		public static void Assert(bool f)
		{
			if (f)
				return;

			Console.WriteLine("Assertion failed");
			System.Diagnostics.Debug.Fail("Assertion failed");
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			TestMysqlDataListTable.Run();
		}
	}
}
