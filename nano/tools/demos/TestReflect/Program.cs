using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Reft
{
	static class Test
	{
		public static void Assert(bool f)
		{
			Debug.Assert(f);
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			TestClassAttr.Run();
			TestMethodAttr.Run();
		}
	}
}
