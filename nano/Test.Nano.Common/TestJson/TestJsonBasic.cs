using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
	class TestJsonBasic
	{
		static void testValueCopy()
		{
			JsonNode s = new JsonNode(1);
			JsonNode t = new JsonNode(s, false);
			Test.Assert(s != t && t.IntValue == 1);
			t = new JsonNode(s, true);
			Test.Assert(s != t && t.IntValue == 1);

			s = new JsonNode("Hello");
			t = new JsonNode(s, false);
			Test.Assert(s != t && t.TextValue == "Hello");
			t = new JsonNode(s, true);
			Test.Assert(s != t && t.TextValue == "Hello");

			s = new JsonNode(3.14);
			t = new JsonNode(s, false);
			Test.Assert(s != t && t.FloatValue == 3.14);
			t = new JsonNode(s, true);
			Test.Assert(s != t && t.FloatValue == 3.14);

			s = new JsonNode(true);
			t = new JsonNode(s, false);
			Test.Assert(s != t && t.BoolValue);
			t = new JsonNode(s, true);
			Test.Assert(s != t && t.BoolValue);

			s = new JsonNode(JsonNodeType.Null);
			t = new JsonNode(s, false);
			Test.Assert(s != t && t.NodeType == JsonNodeType.Null);
			t = new JsonNode(s, true);
			Test.Assert(s != t && t.NodeType == JsonNodeType.Null);
		}

		static void testListCopy()
		{
			var a = new JsonNode(1);
			var b = new JsonNode("Hello");
			var x = new JsonNode(JsonNodeType.NodeList);
			x.AddListItem(a);
			x.AddListItem(b);

			var y = new JsonNode(x, false);
			Test.Assert(x != y && y.NodeType == JsonNodeType.NodeList && y.ChildCount == 2);
			Test.Assert(y[0] == a && y[1] == b);

			y = new JsonNode(x, true);
			Test.Assert(x != y && y.NodeType == JsonNodeType.NodeList && y.ChildCount == 2);
			Test.Assert(y[0] != a && y[0].IntValue == 1);
			Test.Assert(y[1] != b && y[1].TextValue == "Hello");
		}

		static void testDictCopy()
		{
			var a = new JsonNode(1) { Name = "a" };
			var b = new JsonNode("Hello") { Name = "b" };
			var x = new JsonNode(JsonNodeType.Dictionary);
			x.AddDictionaryItem(a);
			x.AddDictionaryItem(b);

			var y = new JsonNode(x, false);
			Test.Assert(x != y && y.NodeType == JsonNodeType.Dictionary && y.ChildCount == 2);
			Test.Assert(y["a"] == a && y["b"] == b);

			y = new JsonNode(x, true);
			Test.Assert(x != y && y.NodeType == JsonNodeType.Dictionary && y.ChildCount == 2);
			Test.Assert(y["a"] != a && y["a"].IntValue == 1);
			Test.Assert(y["b"] != b && y["b"].TextValue == "Hello");
		}

		static void testTreeCopy()
		{
			var a = new JsonNode(1) { Name = "a" };
			var b = new JsonNode(JsonNodeType.Dictionary);
			b.AddDictionaryItem(a);
			var c = new JsonNode(JsonNodeType.NodeList);
			c.AddListItem(b);

			var d = new JsonNode(c, true);
			Test.Assert(d != c && d.NodeType == JsonNodeType.NodeList && d.ChildCount == 1);
			var x = d[0];
			Test.Assert(x != b && x.NodeType == JsonNodeType.Dictionary && x.ChildCount == 1);
			x = x["a"];
			Test.Assert(x != a && x.IntValue == 1);

			d = new JsonNode(c, false);
			Test.Assert(d != c && d.NodeType == JsonNodeType.NodeList && d.ChildCount == 1);
			x = d[0];
			Test.Assert(x == b);
			x = x["a"];
			Test.Assert(x == a);
		}

		public static void Run()
		{
			Console.WriteLine("TestJson.TestJsonBasic");
			testValueCopy();
			testListCopy();
			testDictCopy();
			testTreeCopy();
		}
	}
}
