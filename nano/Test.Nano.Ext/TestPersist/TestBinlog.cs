using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.Ext.Marshal;
using Nano.Ext.Persist;
using Nano.UnitTest;

namespace TestExt
{
	class TestBinlog
	{
		class User
		{
			public string Name;

			public int Index;

			public void SetName(string name, BinlogAccept change)
			{
				Name = name;
				var o = new Dictionary<string, object> { { "c", "setusername" }, { "index", Index }, { "name", name } };
				change.WriteObject(o);
			}
		}

		class TestData
		{
			public List<User> Users;

			public User AddUser(string name, BinlogAccept change)
			{
				var user = new User() { Name = name, Index = Users.Count };
				Users.Add(user);

				var o = new Dictionary<string, object> { { "c", "adduser" }, { "name", name } };
				change.WriteObject(o);
				return user;
			}
		}

		class Access : BinlogAccess
		{
			protected TestData m_data;

			public TestData Data
			{
				get { return m_data; }
			}

			void BinlogAccess.LoadStarted() { }

			void BinlogAccess.MakeNew()
			{
				Test.Assert(m_data == null);
				m_data = new TestData();
				m_data.Users = new List<User>();
			}

			void BinlogAccess.LoadMap(Stream stream)
			{
				Test.Assert(m_data == null);
				((BinlogAccess)this).MakeNew();

				var tr = new StreamReader(stream, Encoding.UTF8);
				string s;
				while ((s = tr.ReadLine()) != null)
				{
					var jnode = JsonParser.ParseText(s);
					switch (jnode["c"].TextValue)
					{
						case "user":
							{
								var user = new User();
								user.Name = jnode["name"].TextValue;
								user.Index = m_data.Users.Count;
								m_data.Users.Add(user);
							}
							break;
						default:
							throw new ArgumentException();
					}
				}
				tr.Close();
			}

			protected void AcceptBinlog(JsonNode jnode)
			{
				switch (jnode["c"].TextValue)
				{
					case "adduser":
						{
							var user = new User();
							user.Name = jnode["name"].TextValue;
							user.Index = m_data.Users.Count;
							m_data.Users.Add(user);
						}
						break;
					case "setusername":
						{
							int index = (int)jnode["index"].IntValue;
							var user = m_data.Users[index];
							user.Name = jnode["name"].TextValue;
						}
						break;
					default:
						throw new ArgumentException();
				}
			}

			void BinlogAccess.LoadLog(JsonModelLoader jld, Stream stream)
			{
				jld.Load(stream, AcceptBinlog);
			}

			void BinlogAccess.LoadCompleted() { }

			void BinlogAccess.SaveMap(Stream stream)
			{
				var tw = new StreamWriter(stream, Encoding.UTF8);
				foreach (var user in m_data.Users)
				{
					var o = new Dictionary<string, string>() { { "c", "user" }, { "name", user.Name } };
					string s = JsonModel.Dumps(o);
					tw.WriteLine(s);
				}
				tw.Close();
			}
		}

		const string path = "test";

		void clean_env(bool create)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
				System.Threading.Thread.Sleep(100);    // wait for os
			}
			if (create)
				Directory.CreateDirectory(path);
		}

		void open_store(out Access acc, out TestData data, out BinlogStore store)
		{
			acc = new Access();
			store = new BinlogStore(acc, path);
			store.Open();
			data = acc.Data;
		}

		void test_simple_routine(bool interrupt)
		{
			clean_env(true);

			Access acc;
			TestData data;
			BinlogStore store;

			open_store(out acc, out data, out store);
			var names = new string[] { "Yang", "Zhang", "Zhao" };
			foreach (var name in names)
				data.AddUser(name, store.ChangeAccept);
			store.Close(interrupt);

			open_store(out acc, out data, out store);
			Test.Assert(data.Users.Count == 3 && data.Users[0].Name == "Yang" && data.Users[2].Name == "Zhao");
			foreach (var user in data.Users)
				user.SetName(user.Name.ToLowerInvariant(), store.ChangeAccept);
			data.AddUser("wang", store.ChangeAccept);
			store.Close(interrupt);

			open_store(out acc, out data, out store);
			Test.Assert(data.Users.Count == 4 && data.Users[0].Name == "yang" && data.Users[3].Name == "wang");
			store.Close();

			clean_env(false);
		}

		public static void Run()
		{
			Console.WriteLine("TestPersist.TestBinlog");
			var o = new TestBinlog();
			o.test_simple_routine(false);
			o.test_simple_routine(true);
		}
	}
}
