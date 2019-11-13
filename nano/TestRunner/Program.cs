using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

namespace TestRunner
{
	class Program
	{
		static Type FindType(Type[] vts, string name)
		{
			foreach (var vt in vts)
			{
				if (vt.Name == name)
					return vt;
			}
			return null;
		}

		static bool ShowLog = true;
		static bool WaitKey = true;

		static void LogLine(string s)
		{
			if (ShowLog)
				Console.WriteLine(s);
		}

		static void Main(string[] args)
		{
			string path = Path.GetFullPath(args[0]);
			try
			{
				LogLine("Loading " + path);
				var asm = Assembly.LoadFrom(path);
				// LoadFile failed when loading Nuts.Script.dll which has a reference to Project Nuts.CodeModel.
				// The exception is "failed to load Nuts.CodeModel.dll".
				// Replacing it with LoadFrom works.
				// See also: https://stackoverflow.com/questions/20605312/could-not-load-file-or-assembly-error-on-gettypes-method

				LogLine("Finding TestProject Class");
				var vts = asm.GetTypes();
				var vt = FindType(vts, "TestProject");
				if (vt == null)
					throw new Exception("Class TestProject not found");

				LogLine("Finding Run Method");
				var m = vt.GetMethod("Run");
				if (m == null)
					throw new Exception("Method Run not found");

				LogLine("Invoking");
				m.Invoke(null, null);
				LogLine("Completed");
			}
			catch (Exception e)
			{
				LogLine(e.GetType().FullName + " raised");
				LogLine(e.Message);
				LogLine("---------- Stack trace ----------");
				LogLine(e.StackTrace);
				LogLine("---------------------------------");
			}

			if (WaitKey)
			{
				Console.WriteLine("Press any key ...");
				Console.ReadKey(true);
			}
		}
	}
}
