using System;
using System.Collections.Generic;
using System.Text;
using Nano.Lexical;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
	class TestJsonParse
	{
		static void AssertFailed(string s)
		{
			Test.AssertExceptionType<LexicalException>(() => JsonParser.ParseText(s));
		}

		static void TestStringValue()
		{
			string s = "\"abc\"";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.String && root.TextValue == "abc");
			Test.Assert((string)root == "abc");

			s = " \" abc \" ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.String && root.TextValue == " abc ");

			s = "\"\"";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.String && root.TextValue == "");

			AssertFailed("\"abc");
			AssertFailed("abc");
		}

		static void TestIntValue()
		{
			string s = "123";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Integer && root.IntValue == 123);
			Test.Assert((long)root == 123 && (int)root == 123);

			s = " 123 ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Integer && root.IntValue == 123);

			s = "-1";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Integer && root.IntValue == -1);

			AssertFailed("123a");
		}

		static bool FloatEqual(double a, double b)
		{
			return Math.Abs((a - b) / (a + b)) < 1e-12;
		}

		static void TestDblValue()
		{
			string s = "123.0";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Float && root.FloatValue == 123.0);
			Test.Assert((double)root == 123.0 && (float)root == 123.0);

			s = " -123. ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Float && root.FloatValue == -123.0);

			s = "+0.3";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Float && FloatEqual(root.FloatValue, 0.3));

			s = "4E-05";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Float && FloatEqual(root.FloatValue, 0.00004));

			AssertFailed("123.x");
		}

		static void TestBooleanValue()
		{
			string s = "true";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Boolean && root.BoolValue && (bool)root);

			s = " TrUe ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Boolean && root.BoolValue);

			s = " fAlSe ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Boolean && !root.BoolValue);

			AssertFailed("tru");
			AssertFailed("truely");
		}

		static void TestNoneValue()
		{
			string s = "null";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Null && root.IsNull);

			s = " NuLl ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Null && root.IsNull);

			AssertFailed("nul");
			AssertFailed("nully");
		}

		static void TestSimpleDictionary()
		{
			string s = "{\"a\":\"b\"}";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Dictionary && root.ChildCount == 1);
			JsonNode node = root["a"];
			Test.Assert(node.NodeType == JsonNodeType.String && node.Name == "a" && node.TextValue == "b");

			s = " { \"a\" : \"1\" , \"b\":\"\" } ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Dictionary && root.ChildCount == 2);
			node = root["a"];
			Test.Assert(node.NodeType == JsonNodeType.String && node.TextValue == "1");
			node = root["b"];
			Test.Assert(node.NodeType == JsonNodeType.String && node.TextValue == "");

			s = "{}";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Dictionary && root.ChildCount == 0);

			AssertFailed("{");
			AssertFailed("{a");
			AssertFailed("{\"a");
			AssertFailed("{\"a\"");
			AssertFailed("{\"a\":");
			AssertFailed("{\"a\":b");
			AssertFailed("{\"a\":\"b");
			AssertFailed("{\"a\":\"b\"");
			AssertFailed("{\"a\":\"b\",");
			AssertFailed("{\"a\":\"b\",}");

			AssertFailed("{,}");
			AssertFailed("{,\"a\":\"b\"}");
			AssertFailed("}");
			AssertFailed("{]");
		}

		static void TestSimpleList()
		{
			string s = "[\"a\"]";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.NodeList && root.ChildCount == 1);
			JsonNode node = root[0];
			Test.Assert(node.NodeType == JsonNodeType.String && node.Name == null && node.TextValue == "a");

			s = " [ \"a\" , \"\" ] ";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.NodeList && root.ChildCount == 2);
			node = root[0];
			Test.Assert(node.NodeType == JsonNodeType.String && node.TextValue == "a");
			node = root[1];
			Test.Assert(node.NodeType == JsonNodeType.String && node.TextValue == "");

			s = "[]";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.NodeList && root.ChildCount == 0);

			AssertFailed("[");
			AssertFailed("[a");
			AssertFailed("[\"a");
			AssertFailed("[\"a\"");
			AssertFailed("[\"a\",");
			AssertFailed("[\"a\",]");

			AssertFailed("[,}");
			AssertFailed("[,\"a\"]");
			AssertFailed("[");
			AssertFailed("[}");
		}

		static void TestRemoveItem()
		{
			string s = "[\"a\", true, 1]";
			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.NodeList && root.ChildCount == 3);

			root.DeleteChildItem(1);
			Test.Assert(root.ChildCount == 2 && root[1].IntValue == 1);

			s = "{\"a\":1,\"b\":2}";
			root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Dictionary && root.ChildCount == 2);

			root.DeleteChildItem("a");
			Test.Assert(root.ChildCount == 1 && root["b"].IntValue == 2);
		}

		static void TestComplextSample()
		{
			string s =
				"{" +
				"  \"return\":{" +
				"    \"u\":[" +
				"      {\"u\":\"http://118.144.67.133/ufa/\",\"t\":\"2\"}," +
			    "      {\"u\":\"http://61.189.5.13/ufa/\",\"t\":\"1\"}" +
				"    ]," +
				"    \"b\":[" +
				"      {" +
				"        \"id\":\"0\"," +
				"        \"u\":[\"0\",\"1\"]," +
				"        \"index\":\"1\"" +
				"      }" +
				"    ]" +
				"  }," +
				"  \"code\":\"Ok\"" +
				"}";

			JsonNode root = JsonParser.ParseText(s);
			Test.Assert(root.NodeType == JsonNodeType.Dictionary && root.ChildCount == 2);
			
			JsonNode node1 = root["return"];	// \[return]
			Test.Assert(node1.NodeType == JsonNodeType.Dictionary && node1.ChildCount == 2);

			JsonNode node2 = node1["u"];	// \[return]\[u]
			Test.Assert(node2.NodeType == JsonNodeType.NodeList && node2.ChildCount == 2);

			JsonNode node3 = node2[0];	// \[return]\[u]\[0]
			Test.Assert(node3.NodeType == JsonNodeType.Dictionary && node3.ChildCount == 2);
			Test.Assert(node3["u"].TextValue == "http://118.144.67.133/ufa/" && node3["t"].TextValue == "2");

			node3 = node2[1]; 	// \[return]\[u]\[1]
			Test.Assert(node3["u"].TextValue == "http://61.189.5.13/ufa/" && node3["t"].TextValue == "1");

			node2 = node1["b"];	// \[return]\[b]
			Test.Assert(node2.NodeType == JsonNodeType.NodeList && node2.ChildCount == 1);

			node3 = node2[0];	// \[return]\[b]\[0]
			Test.Assert(node3.NodeType == JsonNodeType.Dictionary && node3.ChildCount == 3);
			Test.Assert(node3["id"].TextValue == "0" && node3["index"].TextValue == "1");

			JsonNode node4 = node3["u"];	// \[return]\[b]\[0]\[u]
			Test.Assert(node4.NodeType == JsonNodeType.NodeList && node4.ChildCount == 2);
			Test.Assert(node4[0].TextValue == "0" && node4[1].TextValue == "1");

			node1 = root["code"];	// \[code]
			Test.Assert(node1.NodeType == JsonNodeType.String && node1.TextValue == "Ok");
		}

		public static void Run()
		{
			Console.WriteLine("TestJson.TestJsonParse");
			TestStringValue();
			TestIntValue();
			TestDblValue();
			TestBooleanValue();
			TestNoneValue();
			TestSimpleDictionary();
			TestSimpleList();
			TestRemoveItem();
			TestComplextSample();
		}
	}
}
