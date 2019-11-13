using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace TestExt
{
	class TestJsonModel
	{
		void testDumpValue()
		{
			_test(123, new JsonNode(123));
			_test(123l, new JsonNode(123l));
			_test(123f, new JsonNode(123f));
			_test(123d, new JsonNode(123d));
			_test("hello", new JsonNode("hello"));
			_test("", new JsonNode(""));
			_test(true, new JsonNode(true));
			_test(false, new JsonNode(false));
			_test(null, new JsonNode(JsonNodeType.Null));
		}

		class CX
		{
			public string name;
			public int value;
		}

		class CY
		{
			string m_name = "Not used";
			public string name { get; set; }
			public int value { get; set; }
		}

		void testDumpObject()
		{
			var jn = new JsonNode(JsonNodeType.Dictionary);
			jn.AddChildItem(new JsonNode("Test") { Name = "name" });
			jn.AddChildItem(new JsonNode(123) { Name = "value" });

			_test(new CX { name = "Test", value = 123 }, jn);
			_test(new CY { name = "Test", value = 123 }, jn);
			_test(new { name = "Test", value = 123 }, jn);
		}

		void testDumpCollection()
		{
			var jn = new JsonNode(JsonNodeType.Dictionary);
			jn.AddChildItem(new JsonNode("Test") { Name = "name" });
			jn.AddChildItem(new JsonNode(123) { Name = "value" });
			_test(new Dictionary<string, object> { { "value", 123 }, { "name", "Test" } }, jn);

			jn = new JsonNode(JsonNodeType.NodeList);
			jn.AddChildItem(new JsonNode("Test"));
			jn.AddChildItem(new JsonNode(123));
			_test(new List<object> { "Test", 123 }, jn);

			_test(new object[] { "Test", 123 }, jn);

			jn.DeleteChildItem(0);
			jn.AddChildItem(new JsonNode(456));
			_test(new object[] { 123, 456 }, jn);
		}

		void _test(object o, JsonNode jn)
		{
			var jnx = JsonModel.Dump(o);
			Test.Assert(JsonModel.Equals(jnx, jn));
		}

		public static void Run()
		{
			Console.WriteLine("TestMarshal.TestJsonModel");
			var o = new TestJsonModel();
			o.testDumpValue();
			o.testDumpObject();
			o.testDumpCollection();
		}
	}
}
