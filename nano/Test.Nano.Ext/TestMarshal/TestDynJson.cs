using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace TestExt
{
	// DynJsonNode is no longer supported, use Nano.Nuts.DObject instead

	/*
	class TestDynJson
	{
		void testGetValues()
		{
			var jn = DynJson.Dump(123);
			int vi = jn;
			long vl = jn;
			Test.Assert(vi == 123 && vl == 123L);
			Test.Assert(jn == 123 && jn == 123L); // DynNode 必须放在二元操作符左边

			jn = DynJson.Dump(123f);
			float vf = jn;
			double vd = jn;
			Test.Assert(vf == 123f && vd == 123d);
			Test.Assert(jn == 123f && jn == 123d);

			jn = DynJson.Dump(123);
			vf = jn;
			vd = jn;
			Test.Assert(vf == 123f && vd == 123d);
			Test.Assert(jn == 123f && jn == 123d);

			jn = DynJson.Dump("Hello");
			string vs = jn;
			Test.Assert(vs == "Hello");
			Test.Assert(jn == "Hello");

			jn = DynJson.Dump(true);
			bool vb = jn;
			Test.Assert(vb);
			Test.Assert((bool)jn); // Test.Assert(jn);
			if (!jn)
				Test.Fail();

			jn = DynJson.Dump(false);
			Test.Assert(!jn);
			if (jn)
				Test.Fail();

			jn = DynJson.Dump(null);
			Test.Assert(jn.IsNull()); // 不能使用 jn == null
		}

		void testGetItems()
		{
			var jn = DynJson.Dump(new Dictionary<string, object> { { "name", "Orange" }, { "value", 12 } });
			string vs = jn.name;
			long vl = jn.value;
			Test.Assert(vs == "Orange" && vl == 12);
			Test.Assert(jn.name == "Orange" && jn.value == 12);
			Test.Assert(jn["name"] == "Orange" && jn["value"] == 12);

			jn = DynJson.Dump(new { name = "Orange", value = 12 });
			Test.Assert(jn.name == "Orange" && jn.value == 12);

			jn = DynJson.Dump(new { item = new { name = "Orange", value = 12 } });
			Test.Assert(jn.item.name == "Orange");
			jn = jn.item;
			Test.Assert(jn.name == "Orange");

			jn = DynJson.Dump(new List<object> { "red", "orange" });
			vs = jn[0];
			Test.Assert(vs == "red" && jn[1] == "orange");

			jn = DynJson.Dump(new[] { "red", "orange" });
			Test.Assert(jn[0] == "red" && jn[1] == "orange");

			jn = DynJson.Dump(new object[] { "red", new { name = "orange" } });
			Test.Assert(jn[1].name == "orange");
		}

		void testEnum()
		{
			var jn = DynJson.Dump(new object[] { "red", "orange" });
			var s = new StringBuilder();
			foreach (var sub in jn.List())
				s.Append((string)sub).Append(',');
			Test.Assert(s.ToString() == "red,orange,");
		}

		void testJsonStr()
		{
			JsonNode jnode = DynJson.Dump(new Dictionary<string, object> { }).GetNode();
			Test.Assert(jnode.NodeType == JsonNodeType.Dictionary && jnode.ChildCount == 0);

			var str = DynJson.Dumps(new { name = "Orange", value = 12 });
			Test.Assert(str == "{\"name\":\"Orange\",\"value\":12}");

			var jn = DynJson.Loads(str);
			Test.Assert(jn.name == "Orange" && jn.value == 12);
		}

		public static void Run()
		{
			Console.WriteLine("TestMarshal.TestDynJson");
			var o = new TestDynJson();
			o.testGetValues();
			o.testGetItems();
			o.testEnum();
			o.testJsonStr();
		}
	}
	*/
}
