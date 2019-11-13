using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nano.Nuts;

namespace UpdateVersion
{
	class Config
	{
		public long Major, Minor;
		public List<string> Dirs;
		public List<string> Exludes;

		public bool IsExclude(string path) => Exludes.Exists(x => x == path);

		public static Config Load(string path)
		{
			string body;
			using (var tr = new StreamReader(path))
				body = tr.ReadToEnd();
			dynamic o = DObject.ImportJson(body);

			var cfg = new Config
			{
				Major = o["major"], Minor = o["minor"]
			};

			var odirs = o["dirs"];
			cfg.Dirs = new List<string>();
			foreach (dynamic oitem in odirs.List())
			{
				var dir = (string)oitem;
				dir = Path.GetFullPath(dir);
				cfg.Dirs.Add(dir);
			}

			odirs = o["excludes"];
			cfg.Exludes = new List<string>();
			foreach (dynamic oitem in odirs.List())
			{
				var dir = (string)oitem;
				dir = Path.GetFullPath(dir);
				cfg.Exludes.Add(dir);
			}

			return cfg;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			var cfg = Config.Load(args.Length != 0 ? args[0] : "config.txt");
			foreach (var path in cfg.Dirs)
			{
				Console.WriteLine("Dir " + path);
				Inspect(cfg, path);
			}
		}

		static void Inspect(Config cfg, string path)
		{					
			var pathAsm = Path.Combine(path, "AssemblyInfo.cs");
			if (File.Exists(pathAsm))
				InspectFile(cfg, pathAsm);

			var ss = Directory.GetDirectories(path);
			foreach (var s in ss)
			{
				var name = Path.GetFileName(s);
				if (name == "bin" || name == "obj" || name[0] == '.')
					continue;
				if (cfg.IsExclude(s))
					continue;
				Inspect(cfg, s);
			}
		}

		static void InspectFile(Config cfg, string path)
		{
			Console.WriteLine("File " + path);

		}
	}
}
