using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Ext.Persist;
using Nano.UnitTest;

namespace TestExt
{
	class TestBom
	{
		class Company : BomObject
		{
			BomList<User> m_users;
			BomDictionary<string, User> m_umap;

			public Company(BomStore store) : base(store)
			{
			}

			public BomList<User> Users
			{
				get { return m_users; }
				set { SetValue("m_users", m_users = value); }
			}

			public BomDictionary<string, User> UserMap
			{
				get { return m_umap; }
				set { SetValue("m_umap", m_umap = value); }
			}

			public override bool SaverGetValueFields(Dictionary<string, object> map)
			{
				map.Add("m_users", m_users);
				map.Add("m_umap", m_umap);
				return true;
			}
		}

		class User : BomObject
		{
			public string m_name;

			public User(BomStore store) : base(store)
			{
			}

			public string Name
			{
				get { return m_name; }
				set { SetValue("m_name", m_name = value); }
			}

			public override bool SaverGetValueFields(Dictionary<string, object> map)
			{
				map.Add("m_name", m_name);
				return true;
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

		void testSimple(bool interrupt)
		{
			clean_env(true);

			var fta = new Nano.Storage.LocalFileTreeAccess(path);
			var store = new BomStore(fta.Root, typeof(Company));
			store.Open();
			var com = (Company)store.Root;
			Test.Assert(com.Users == null);
			com.Users = new BomList<User>(store);
			com.UserMap = new BomDictionary<string, User>(store);
			string[] names = new string[] { "Yang", "Zhang", "Zhao" };
			foreach (string name in names)
			{
				var user = new User(store) { Name = name };
				com.Users.Add(user);
				com.UserMap.Add(name, user);
			}
			store.Close(interrupt);

			store = new BomStore(fta.Root, typeof(Company));
			store.Open();
			com = (Company)store.Root;
			var users = com.Users;
			Test.Assert(users.Count == 3 && users[0].Name == "Yang" && users[2].Name == "Zhao");
			var umap = com.UserMap;
			Test.Assert(umap.Count == 3 && umap["Yang"].Name == "Yang" && umap["Zhao"].Name == "Zhao");

			foreach (var user in users)
				user.Name = user.Name.ToLowerInvariant();
			var userNew = new User(store) { Name = "wang" };
			users.Add(userNew);
			umap.Add("Wang", userNew);
			store.Close(interrupt);

			store = new BomStore(fta.Root, typeof(Company));
			store.Open();
			com = (Company)store.Root;
			users = com.Users;
			Test.Assert(users.Count == 4 && users[0].Name == "yang" && users[2].Name == "zhao" && users[3].Name == "wang");
			umap = com.UserMap;
			Test.Assert(umap.Count == 4 && umap["Yang"].Name == "yang" && umap["Wang"].Name == "wang");
			store.Close();

			clean_env(false);
		}

		public static void Run()
		{
			Console.WriteLine("TestPersist.TestBom");
			var o = new TestBom();
			o.testSimple(false);
			o.testSimple(true);
		}
	}
}
