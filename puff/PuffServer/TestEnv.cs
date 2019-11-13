using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puff.Server.UnitTest
{
	public class TestProject
	{
		public static void Run()
		{
			Console.WriteLine("Puff.Server Test Suites");
			Console.WriteLine("--------------------");
            // Puff.Marshal
            Marshal.TestJsonObjectBuilder.Run();
            Marshal.TestJsonModelBuilder.Run();
            // Puff.Model
            Model.TestMethodInBuilder.Run();
            Model.TestMethodOutBuilder.Run();
			Console.WriteLine("--------------------\n");
            Nano.UnitTest.Test.Report();
		}
	}
}
