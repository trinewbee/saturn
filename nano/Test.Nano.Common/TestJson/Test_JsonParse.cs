using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
    class Test_JsonParse
    {
		static void assertStringEqual(string x, string y) => Test.Assert(string.CompareOrdinal(x, y) == 0);

		static void AssertFailed(string text)
        {
            Test.AssertExceptionType<JException>(() => JNode.Parse(text));
        }

        static void TestStringValue()
        {
            string s = "\"abc\"";
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsString);
            assertStringEqual(root.TextValue, "abc");
            assertStringEqual((string)root, "abc");

            s = " \" abc \" ";
            root = JNode.Parse(s);
            Test.Assert(root.IsString);
            assertStringEqual(root.TextValue, " abc ");

            s = "\"\"";
            root = JNode.Parse(s);
            Test.Assert(root.IsString);
            assertStringEqual(root.TextValue, "");

            s = string.Format("\"{0}\"", @"c:\\b");
            root = JNode.Parse(s);
            Test.Assert(root.IsString);
            assertStringEqual(root.TextValue, @"c:\b");

            AssertFailed("\"abc");
            AssertFailed("abc");
        }

        static void TestIntValue()
        {
            string s = "123";
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsInteger);
            Test.Assert(root.IntValue == 123);
            Test.Assert((long)root == 123 && (int)root == 123);

            s = " 123 ";
            root = JNode.Parse(s);
            Test.Assert(root.IsInteger);
            Test.Assert(root.IntValue == 123);

            s = "-1";
            root = JNode.Parse(s);
            Test.Assert(root.IsInteger);
            Test.Assert(root.IntValue == -1);

            AssertFailed("123a");
        }

        static bool FloatEqual(double a, double b)
        {
            return Math.Abs((a - b) / (a + b)) < 1e-12;
        }

        static void TestDblValue()
        {
            string s = "123.0";
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsFloat);
            Test.Assert(root.FloatValue == 123.0);
            Test.Assert((double)root == 123.0 && (float)root == 123.0);

            s = " -123. ";
            root = JNode.Parse(s);
            Test.Assert(root.IsFloat);
            Test.Assert(root.FloatValue == -123.0);

            s = "+0.3";
            root = JNode.Parse(s);
            Test.Assert(root.IsFloat);
            Test.Assert(FloatEqual(root.FloatValue, 0.3));

			s = "4E-05";
			root = JNode.Parse(s);
			Test.Assert(FloatEqual(root.FloatValue, 0.00004));

			AssertFailed("123.x");
        }

        static void TestBooleanValue()
        {
            string s = "true";
            JNode root = JNode.Parse(s);
            root = JNode.Parse(s);
            Test.Assert(root.IsBoolean);
            Test.Assert(root.BoolValue);
            Test.Assert((bool)root);

            s = " TrUe ";
            root = JNode.Parse(s);
            Test.Assert(root.IsBoolean);
            Test.Assert(root.BoolValue);

            s = " fAlSe ";
            root = JNode.Parse(s);
            Test.Assert(root.IsBoolean);
            Test.Assert(!root.BoolValue);

            AssertFailed("tru");
            AssertFailed("truely");
        }

        static void TestNoneValue()
        {
            string s = "null";
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsNull);

            s = " NuLl ";
            root = JNode.Parse(s);
            Test.Assert(root.IsNull);

            AssertFailed("nul");
            AssertFailed("nully");
        }

        static void TestSimpleDictionary()
        {
            string s = "{\"a\":\"b\"}";
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsObject);
            Test.Assert(root.Count == 1);

            JNode node = root["a"];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "b");

            s = " { \"a\" : \"1\" , \"b\":\"\" } ";
            root = JNode.Parse(s);
            Test.Assert(root.IsObject);
            Test.Assert(root.Count == 2);
            node = root["a"];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "1");
            node = root["b"];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "");

            s = "{}";
            root = JNode.Parse(s);
            Test.Assert(root.IsObject);
            Test.Assert(root.Count == 0);

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
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsArray);
            Test.Assert(root.Length == 1);
            JNode node = root[0];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "a");

            s = " [ \"a\" , \"\" ] ";
            root = JNode.Parse(s);
            Test.Assert(root.IsArray);
            Test.Assert(root.Length == 2);
            node = root[0];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "a");
            node = root[1];
            Test.Assert(node.IsString);
            assertStringEqual(node.TextValue, "");

            s = "[]";
            root = JNode.Parse(s);
            Test.Assert(root.IsArray);
            Test.Assert(root.Length == 0);

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
            JNode root = JNode.Parse(s);
            Test.Assert(root.IsArray);
            Test.Assert(root.Length == 3);

            root.RemoveAt(1);
            Test.Assert(root.Length == 2);
            Test.Assert(root[1].IntValue == 1);

            s = "{\"a\":1,\"b\":2}";
            root = JNode.Parse(s);
            Test.Assert(root.IsObject);
            Test.Assert(root.Count == 2);

            root.Remove("a");
            Test.Assert(root.Count == 1);
            Test.Assert(root["b"].IntValue == 2);
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

            JNode root = JNode.Parse(s);
            Test.Assert(root.IsObject);
            Test.Assert(root.Count == 2);

            JNode node1 = root["return"];    // \[return]
            Test.Assert(node1.IsObject);
            Test.Assert(node1.Count == 2);

            JNode node2 = node1["u"];    // \[return]\[u]
            Test.Assert(node2.IsArray);
            Test.Assert(node2.Length == 2);

            JNode node3 = node2[0];  // \[return]\[u]\[0]
            Test.Assert(node3.IsObject);
            Test.Assert(node3.Count == 2);
            assertStringEqual("http://118.144.67.133/ufa/", node3["u"].TextValue);
            assertStringEqual("2", node3["t"].TextValue);

            node3 = node2[1];   // \[return]\[u]\[1]
            assertStringEqual("http://61.189.5.13/ufa/", node3["u"].TextValue);
            assertStringEqual("1", node3["t"].TextValue);

            node2 = node1["b"]; // \[return]\[b]
            Test.Assert(node2.IsArray);
            Test.Assert(node2.Length == 1);

            node3 = node2[0];   // \[return]\[b]\[0]
            Test.Assert(node3.IsObject);
            Test.Assert(node3.Count == 3);
            assertStringEqual("0", node3["id"].TextValue);
            assertStringEqual("1", node3["index"].TextValue);

            JNode node4 = node3["u"];    // \[return]\[b]\[0]\[u]
            Test.Assert(node4.IsArray);
            Test.Assert(node4.Length == 2);
            assertStringEqual("0", node4[0].TextValue);
            assertStringEqual("1", node4[1].TextValue);

            node1 = root["code"];   // \[code]
            Test.Assert(node1.IsString);
            assertStringEqual("Ok", node1.TextValue);
        }

        public static void Run()
        {
            Console.WriteLine("Tests.Test.Josn.Net.Test_JNodeParse");
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
