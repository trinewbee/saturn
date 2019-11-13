using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Json;
using Nano.Ext.Marshal;
using Nano.Xapi.Netdisk;
using Nano.Xapi.Netdisk.Http;
using Nano.Xapi.Netdisk.Model;

namespace NanoDeploy
{
	class RimFile
	{
		public long Fid = 0;
		public string Name = null, Path = null;
		public bool IsDir = false;
		public long Size = 0;
		public long SMTime = 0, RSMTime = 0;
		public string Stor = null;
		public List<RimFile> Children = null;
	}

	class RimModel
	{
		int m_gid = 0;
		string m_start = "/";
		RimFile m_root;

		public void Init()
		{
			var server = new ServerConfig() { Host = "cloudhua.com", Port = 8080, Protocal = "http" };
			var proxy = new ProxyConfig { Type = XHttpProxy.TYPE_NONE };

			XClient.Init(server, proxy);

			var nlr = Auth.NameLogin("deploy", "Itnihao123");
			Debug.Assert(nlr.Succeeded);
			XClient.SetToken(nlr.Token);
		}

		public void Sync()
		{
			var ifr = Fs.InfoByPath(m_gid, m_start);
			Debug.Assert(ifr.Succeeded && IsDir(ifr.Item));

			MergeFile(ref m_root, ifr.Item, m_start);
			SyncDir(m_root);
		}

		void SyncDir(RimFile fi)
		{
			Console.WriteLine("List " + fi.Path);
			Debug.Assert(fi.IsDir);
			string prefix = fi.Path != "/" ? (fi.Path + '/') : fi.Path;

			var lsr = Fs.ListByPath(m_gid, fi.Path);
			Debug.Assert(lsr.Succeeded);

			var map = new Dictionary<string, NdFsListResponse.Item>();
			foreach (var item in lsr.Items)
				map.Add(item.Name.ToLowerInvariant(), item);

			for (int i = fi.Children.Count - 1; i >= 0; --i)
			{
				var subfi = fi.Children[i];
				string namelc = subfi.Name.ToLowerInvariant();
				NdFsListResponse.Item item;
				if (map.TryGetValue(namelc, out item))
				{
					string path = prefix + item.Name;
					if (MergeFile(ref subfi, item, path))
						SyncDir(subfi);
					map.Remove(namelc);
				}
				else
					fi.Children.RemoveAt(i);
			}

			foreach (var item in map.Values)
			{
				RimFile subfi = null;
				string path = prefix + item.Name;
				if (MergeFile(ref subfi, item, path))
					SyncDir(subfi);
				fi.Children.Add(subfi);
			}

			fi.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
		}

		static bool IsDir(NdFsListResponse.Item item)
		{
			return (item.Attr & 16) != 0;
		}

		// SyncDir should be called if MergeFile returns true
		static bool MergeFile(ref RimFile fi, NdFsListResponse.Item item, string path)
		{
			if (fi != null)
			{
				if (fi.IsDir)
				{
					if (fi.RSMTime == item.RSMTime)
					{
						return false;
					}
					else if (fi.SMTime == item.SMTime)
					{
						fi.RSMTime = item.RSMTime;
						return true;
					}						
				}
				else
				{
					if (fi.SMTime == item.SMTime)
						return false;
				}
			}

			if (fi == null)
				fi = new RimFile();

			fi.Fid = item.Fid;
			fi.Name = item.Name;
			fi.Path = path;
			fi.IsDir = IsDir(item);
			fi.Size = item.Size;
			fi.SMTime = item.SMTime;
			fi.RSMTime = item.RSMTime;
			fi.Stor = item.Stor;

			if (fi.IsDir)
			{
				if (fi.Children == null)
					fi.Children = new List<RimFile>();
			}
			else
				fi.Children = null;

			return fi.IsDir;
		}

		public void Close()
		{
			Auth.Logout();
		}

		#region Save

		public void Save()
		{
			var tw = new StreamWriter("meta.txt", false, Encoding.UTF8);
			SaveFile(m_root, tw);
			tw.Close();
		}

		void SaveFile(RimFile fi, TextWriter tw)
		{
			string c = fi.IsDir ? "d" : "f";
			Dictionary<string, object> m = new Dictionary<string, object> {
				{ "c", c }, { "id", fi.Fid }, { "n", fi.Name }, {"smt", fi.SMTime }
			};
			if (!fi.IsDir)
			{
				m.Add("s", fi.Size);
				m.Add("stor", fi.Stor);
			}
			else
				m.Add("rsmt", fi.RSMTime);
			tw.WriteLine(JsonModel.Dumps(m));

			if (fi.IsDir)
			{
				foreach (var subfi in fi.Children)
					SaveFile(subfi, tw);

				m = new Dictionary<string, object> { { "c", "ed" } };
				tw.WriteLine(JsonModel.Dumps(m));
			}
		}

		#endregion

		#region Load

		Stack<RimFile> m_fiStk = null;

		public void Load()
		{
			if (!File.Exists("meta.txt"))
				return;

			m_root = null;
			m_fiStk = new Stack<RimFile>();
			var tr = new StreamReader("meta.txt", Encoding.UTF8);
			string s;
			while ((s = tr.ReadLine()) != null)
			{
				JsonNode node = JsonParser.ParseText(s);
				string c = node["c"].TextValue;
				switch (c)
				{
					case "d":
						EnterDir(node);
						break;
					case "f":
						EnterFile(node);
						break;
					case "ed":
						m_fiStk.Pop();
						break;
				}
			}
			tr.Close();

			Debug.Assert(m_root != null && m_fiStk.Count == 0);
			m_fiStk = null;

			DateTime dt = File.GetLastWriteTimeUtc("meta.txt");
			string newName = "meta-" + UnixTimestamp.ToTimestamp(dt) + ".txt";
			File.Move("meta.txt", newName);
		}

		void EnterDir(JsonNode node)
		{
			// {"c":"d","id":27655294418946,"n":"cpp","smt":1471427653865,"rsmt":1471511514683}
			RimFile parent = m_fiStk.Count != 0 ? m_fiStk.Peek() : null;
			RimFile fi = new RimFile();
			fi.Fid = node["id"].IntValue;
			fi.Name = node["n"].TextValue;
			fi.Path = parent != null ? Combine(parent.Path, fi.Name) : m_start;
			fi.IsDir = true;
			fi.Size = 0;
			fi.SMTime = node["smt"].IntValue;
			fi.RSMTime = node["rsmt"].IntValue;
			fi.Stor = null;
			fi.Children = new List<RimFile>();

			if (parent != null)
				parent.Children.Add(fi);
			else
				m_root = fi;

			m_fiStk.Push(fi);
		}

		void EnterFile(JsonNode node)
		{
			// {"c":"f","id":27655294418949,"n":"cbfs.cab","smt":1471427735387,"s":1536465,"stor":"JzfPcBkKIGOPjNZdJtMMni1kkxIAF3HR"}
			RimFile parent = m_fiStk.Peek();
			RimFile fi = new RimFile();
			fi.Fid = node["id"].IntValue;
			fi.Name = node["n"].TextValue;
			fi.Path = parent != null ? Combine(parent.Path, fi.Name) : m_start;
			fi.IsDir = false;
			fi.Size = node["s"].IntValue;
			fi.RSMTime = fi.SMTime = node["smt"].IntValue;
			fi.Stor = node["stor"].TextValue;
			fi.Children = null;

			parent.Children.Add(fi);
		}

		static string Combine(string prefix, string name)
		{
			return prefix != "/" ? prefix + '/' + name : '/' + name;
		}

		#endregion
	}

	class Program
	{
		static void Main(string[] args)
		{
			RimModel model = new RimModel();
			model.Init();
			model.Load();
			model.Sync();
			model.Save();
			model.Close();
		}
	}
}
