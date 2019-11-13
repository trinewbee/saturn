using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Collection;
using Nano.UnitTest;

namespace TestCommon.TestCollection
{
	static class TestListTable
	{
		class TestItem : ListTableItem<string>
		{
			public string Value = null;

			public TestItem(int index, string key, string value)
				: base(index, key)
			{
				Value = value;
			}
		}

		static void testAdd()
		{
			ListTable<string, TestItem> table = new ListTable<string, TestItem>();
			Test.Assert(table.Count == 0 && table[1] == null);
			TestItem a = new TestItem(0, "name", "Zhang");
			table.Add(a);
			Test.Assert(table.Count == 1 && a.Index == 1 && table[1] == a && table["name"] == a);

			TestItem b = new TestItem(3, "birth", "2000");
			table.Add(b);
			Test.Assert(table.Count == 2 && b.Index == 3 && table[3] == b && table["birth"] == b);

			TestItem c = new TestItem(0, "height", "170");
			table.Add(c);
			Test.Assert(table.Count == 3 && c.Index == 4 && table[4] == c && table["height"] == c);

			string indices = "", keys = "";
			foreach (TestItem x in table)
			{
				indices += x.Index.ToString() + ',';
				keys += x.Key + ',';
			}
			Test.Assert(indices == "1,3,4," && keys == "name,birth,height,");

			table.Clear();
			Test.Assert(table.Count == 0);

			TestItem d = new TestItem(0, "name", "Wang");
			table.Add(d);
			Test.Assert(table.Count == 1 && d.Index == 1);
		}

		static void testRemove()
		{
			ListTable<string, TestItem> table = new ListTable<string, TestItem>();
			table.Add(new TestItem(1, "name", "Zhang"));
			table.Add(new TestItem(3, "birth", "2000"));
			Test.Assert(table.Count == 2);

			Test.Assert(table.RemoveAt(1));
			Test.Assert(table.Count == 1 && table[1] == null && table["name"] == null);

			Test.Assert(table.Remove("birth"));
			Test.Assert(table.Count == 0 && table[3] == null && table["birth"] == null);
		}

		public static void Run()
		{
			Console.WriteLine("TestCollection.TestListTable");
			testAdd();
			testRemove();
		}
	}
}
