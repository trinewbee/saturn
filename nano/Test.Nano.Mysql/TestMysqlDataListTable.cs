using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Collection;
using Nano.Data.MySql;
using MySql.Data.MySqlClient;

namespace Test.Nano.Mysql
{
	class TestMysqlDataListTable
	{
		class TestItem : DataTableItem
		{
			[TableField(Size = 32)]
            public string Name = null;

            public uint Birth = 0;

			public TestItem()
			{
			}

			public TestItem(uint id, string name, uint birth) : base(id)
			{
				Name = name;
				Birth = birth;
			}
		}

        class User : DataTableItem
        {
            [TableField(unique = false, indexName = "uk_n", Size = 32)]
            public string name;

            [TableField(unique = true, indexName = "uk_ab")]
            public int birth;

            [TableField(unique = true, indexName = "uk_ab")]
            public int age;

            public User(uint id, string name, int birth, int age) : base(id)
            {
                this.name = name;
                this.birth = birth;
                this.age = age;
            }
            public User()
            {
            }
        }

        SingletonMysqlConnectionFactory m_factory;
		MysqlDataListTable<TestItem> m_table;
        MysqlDataListTable<User> m_userTable;
        void setUp()
		{
			MySqlConnection conn = MySqlKit.Connect("cloudhua.com", 11133, "yang", "sucunkejiNB", "test");
			m_factory = new SingletonMysqlConnectionFactory(conn);
			
			m_table = new MysqlDataListTable<TestItem>(m_factory, "TestItems");
			m_table.DropTable();
			m_table.CreateTable();

            m_userTable = new MysqlDataListTable<User>(m_factory, "User");
            m_userTable.DropTable();
            m_userTable.CreateTable();
		}

		void tearDown()
		{
			m_factory.Dispose();
			m_factory = null;
		}

		void testAdd()
		{
			TestItem item = new TestItem(0, "Wang", 1980);
			item = m_table.Add(item);
			Test.Assert(item.Id == 1 && item.Name == "Wang");

			item = new TestItem(3, "Zhang", 1990);
			item = m_table.Add(item);
			Test.Assert(item.Id == 3 && item.Name == "Zhang");

			item = new TestItem(0, "Li", 2000);
			item = m_table.Add(item);
			Test.Assert(item.Id == 4 && item.Name == "Li");

			Test.Assert(m_table.Count == 3);
			List<TestItem> items = m_table.Select(null);
			Test.Assert(items.Count == 3 && items[0].Id == 1 && items[0].Name == "Wang");
			Test.Assert(items[1].Id == 3 && items[2].Id == 4);

			items = m_table.Select("`Name` like '%g' and `Birth` >= 1990");
			Test.Assert(items.Count == 1 && items[0].Id == 3);

			Test.Assert(m_table.Select("Id = 2").Count == 0);
			Test.Assert(m_table[2] == null && m_table[3].Name == "Zhang");

			m_table.Clear();
			Test.Assert(m_table.Count == 0);
		}

		void testUpdate()
		{
			string[] names = "Wang,Li,Zhang,Liu,Chen,Yang".Split(',');
			foreach (string name in names)
				m_table.Add(new TestItem(0, name, 0));

			TestItem item = m_table.Select("Name='Li'")[0];
			item.Birth = 1990;
			Test.Assert(m_table.Update(item) == 1);
			List<TestItem> items = m_table.Select("Birth!=0");
			Test.Assert(items.Count == 1 && items[0].Name == "Li");

			Test.Assert(m_table.Update(item) == 1);
			items = m_table.Select("Birth!=0");
			Test.Assert(items.Count == 1 && items[0].Name == "Li");

			Test.Assert(m_table.BatchUpdate("Birth=2000", "Name like 'Li%'") == 2);
			items = m_table.Select("Birth != 0");
			Test.Assert(items.Count == 2 && items[0].Name == "Li" && items[1].Name == "Liu");

			Test.Assert(m_table.BatchUpdate("Name='Zhao', Birth=2001", "Name='Yang'") == 1);
			Test.Assert(m_table.Select("Birth=2001").Count == 1);
			Test.Assert(m_table.Select("Name='Yang'").Count == 0 && m_table.Select("Name='Zhao'").Count == 1);

			Test.Assert(m_table.BatchUpdate("Birth=1998", null) == 6);
			Test.Assert(m_table.Select("Birth>=2000").Count == 0 && m_table.Select("Birth=1998").Count == 6);

			m_table.Clear();
		}

		void testRemove()
		{
			string[] names = "Wang,Li,Zhang,Liu,Chen,Yang".Split(',');
			uint[] ids = new uint[names.Length];
			for (int i = 0; i < names.Length; ++i)
				ids[i] = m_table.Add(new TestItem(0, names[i], 0)).Id;

			Func<TestItem, string> t = (TestItem item) => (item.Name);
			Test.Assert(m_table.RemoveAt(ids[1]) == 1 && m_table.Count == 5);
			List<string> names_t = CollectionKit.Transform<TestItem, string>(m_table.Select(null), t);
			Test.Assert(string.Join(",", names_t) == "Wang,Zhang,Liu,Chen,Yang");

			Test.Assert(m_table.RemoveAt(ids[1]) == 0 && m_table.Count == 5);

			Test.Assert(m_table.RemoveRange(ids[2], ids[4]) == 2 && m_table.Count == 3);
			names_t = CollectionKit.Transform<TestItem, string>(m_table.Select(null), t);
			Test.Assert(string.Join(",", names_t) == "Wang,Chen,Yang");

			Test.Assert(m_table.RemoveRange(ids[2], ids[4]) == 0 && m_table.Count == 3);

			Test.Assert(m_table.Remove("`Name` like '%g'") == 2 && m_table.Count == 1);
			names_t = CollectionKit.Transform<TestItem, string>(m_table.Select(null), t);
			Test.Assert(names_t.Count == 1 && names_t[0] == "Chen");

			Test.Assert(m_table.Remove("`Name` like '%g'") == 0 && m_table.Count == 1);
			m_table.Clear();
		}

		void testEnum()
		{
			string value = null;
			Action<TestItem> d = (TestItem item) => value += item.Name + ',';

			foreach (string name in "Wang,Li,Zhang".Split(','))
				m_table.Add(new TestItem(0, name, 0));

			value = "";
			CollectionKit.WalkGeneral(m_table, d);
			Test.Assert(value == "Wang,Li,Zhang,");

			value = "";
			CollectionKit.WalkGeneral(m_table.Enumerate("Name like '%g'"), d);
			Test.Assert(value == "Wang,Zhang,");

			value = "";
			CollectionKit.WalkGeneral(m_table.Enumerate("Name like 'Y%'"), d);
			Test.Assert(value == "");

			m_table.Clear();
		}

        void testIndex()
        {
            // add data
            User user = new User(0, "文梁俊", 10, 24);
            user = m_userTable.Add(user);
            Test.Assert(user.Id > 0 && user.name == "文梁俊");

            user = new User(2, "浪", 11, 23);
            user = m_userTable.Add(user);
            Test.Assert(user.Id == 2 && user.name == "浪");
            Test.Assert(m_userTable.Count == 2);

            try
            {
                user = new User(0, "加", 11, 23);
                user = m_userTable.Add(user);
                Test.Assert(false);
            }
            catch (Exception e)
            {
            }
            
            m_userTable.Clear();
            Test.Assert(m_userTable.Count == 0);
        }

		public static void Run()
		{
			TestMysqlDataListTable o = new TestMysqlDataListTable();
			o.setUp();
			o.testAdd();
			o.testUpdate();
			o.testRemove();
			o.testEnum();
            o.testIndex();
            o.tearDown();
        }
	}
}
