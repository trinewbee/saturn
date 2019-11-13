using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Lexical;
using Nano.Json;
using Nano.Json.Expression;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
	class TestJsonWriter
	{	
		static void TestWriteValues()
		{
			JsonWriter wr = new JsonWriter();
			wr.WriteString(null, "abc");
			Test.Assert(wr.GetString() == "\"abc\"");

            wr.Reset();
            wr.WriteString(null, "\"Hello, \'c\', a\\b");
            Test.Assert(wr.GetString() == @"""\""Hello, 'c', a\\b""");	// single quota ' is not escaped

			wr.Reset();
			wr.WriteInt(null, 1234567890123456789L);
			Test.Assert(wr.GetString() == "1234567890123456789");

			wr.Reset();
			wr.WriteBool(null, true);
			Test.Assert(wr.GetString() == "true");

			wr.Reset();
			wr.WriteBool(null, false);
			Test.Assert(wr.GetString() == "false");

			wr.Reset();
			wr.WriteNull(null);
			Test.Assert(wr.GetString() == "null");

			// With format
			wr.Reset();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.WriteString(null, "abc");
			Test.Assert(wr.GetString() == "\"abc\"");
		}

		static void TestWriteDictionary()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.WriteString("name", "abc");
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"name\":\"abc\"}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.WriteString("name", "abc");
			wr.WriteInt("value", 123);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"name\":\"abc\",\"value\":123}");
		}

		static void TestWriteDictionaryFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginDictionary(null);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.WriteString("name", "abc");
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"name\":\"abc\"\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.WriteString("name", "abc");
			wr.WriteInt("value", 123);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"name\":\"abc\",\n \"value\":123\n}");
		}

		static void TestWriteList()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.EndList();
			Test.Assert(wr.GetString() == "[]");

			wr.Reset();
			wr.BeginList(null);
			wr.WriteString(null, "abc");
			wr.EndList();
			Test.Assert(wr.GetString() == "[\"abc\"]");

			wr.Reset();
			wr.BeginList(null);
			wr.WriteString(null, "abc");
			wr.WriteInt(null, 123);
			wr.EndList();
			Test.Assert(wr.GetString() == "[\"abc\",123]");
		}

		static void TestWriteListFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginList(null);
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.WriteString(null, "abc");
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n \"abc\"\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.WriteString(null, "abc");
			wr.WriteInt(null, 123);
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n \"abc\",\n 123\n]");
		}

		static void TestWriteDictionaryUnderDictionary()
		{
			JsonWriter wr = new JsonWriter();
			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"dict\":{}}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"dict\":{\"key\":123}}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"dict\":{\"key\":123,\"value\":456}}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"dict\":{\"key\":123},\"value\":456}");
		}

		static void TestWriteDictionaryUnderDictionaryFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"dict\":{\n }\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"dict\":{\n  \"key\":123\n }\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"dict\":{\n  \"key\":123,\n  \"value\":456\n }\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginDictionary("dict");
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"dict\":{\n  \"key\":123\n },\n \"value\":456\n}");
		}

		static void TestWriteListUnderDictionary()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"list\":[]}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"list\":[123]}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.WriteInt(null, 456);
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"list\":[123,456]}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\"list\":[123],\"value\":456}");
		}

		static void TestWriteListUnderDictionaryFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"list\":[\n ]\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"list\":[\n  123\n ]\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.WriteInt(null, 456);
			wr.EndList();
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"list\":[\n  123,\n  456\n ]\n}");

			wr.Reset();
			wr.BeginDictionary(null);
			wr.BeginList("list");
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			Test.Assert(wr.GetString() == "{\n \"list\":[\n  123\n ],\n \"value\":456\n}");			
		}

		static void TestWriteDictionaryUnderList()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[{}]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[{\"key\":123}]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[{\"key\":123,\"value\":456}]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.WriteInt(null, 456);
			wr.EndList();
			Test.Assert(wr.GetString() == "[{\"key\":123},456]");
		}

		static void TestWriteDictionaryUnderListFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n {\n }\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n {\n  \"key\":123\n }\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.WriteInt("value", 456);
			wr.EndDictionary();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n {\n  \"key\":123,\n  \"value\":456\n }\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginDictionary(null);
			wr.WriteInt("key", 123);
			wr.EndDictionary();
			wr.WriteInt(null, 456);
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n {\n  \"key\":123\n },\n 456\n]");
		}

		static void TestWriteListUnderList()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[[]]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[[123]]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.WriteInt(null, 456);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[[123,456]]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.WriteInt(null, 456);
			wr.EndList();
			Test.Assert(wr.GetString() == "[[123],456]");
		}

		static void TestWriteListUnderListFormat()
		{
			JsonWriter wr = new JsonWriter();
			wr.IndentChars = " ";
			wr.NewLineChars = "\n";
			wr.BeginList(null);
			wr.BeginList(null);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n [\n ]\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n [\n  123\n ]\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.WriteInt(null, 456);
			wr.EndList();
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n [\n  123,\n  456\n ]\n]");

			wr.Reset();
			wr.BeginList(null);
			wr.BeginList(null);
			wr.WriteInt(null, 123);
			wr.EndList();
			wr.WriteInt(null, 456);
			wr.EndList();
			Test.Assert(wr.GetString() == "[\n [\n  123\n ],\n 456\n]");
		}

		static void TestComplextSample()
		{
			string s =
				"{\n" +
				"  \"return\":{\n" +
				"    \"u\":[\n" +
				"      {\n" +
				"        \"u\":\"http://118.144.67.133/ufa/\",\n" +
				"        \"t\":2\n" +
				"      },\n" +
				"      {\n" +
				"        \"u\":\"http://61.189.5.13/ufa/\",\n" +
				"        \"t\":1\n" +
				"      }\n" +
				"    ],\n" +
				"    \"p\":\"2\"\n" +
				"  },\n" +
				"  \"code\":\"Ok\"\n" +
				"}";

			JsonWriter wr = new JsonWriter();
			wr.IndentChars = "  ";
			wr.NewLineChars = "\n";

			wr.BeginDictionary(null);
			  wr.BeginDictionary("return");
			    wr.BeginList("u");
				  wr.BeginDictionary(null);
				    wr.WriteString("u", "http://118.144.67.133/ufa/");
					wr.WriteInt("t", 2);
				  wr.EndDictionary();
				  wr.BeginDictionary(null);
				    wr.WriteString("u", "http://61.189.5.13/ufa/");
				    wr.WriteInt("t", 1);
				  wr.EndDictionary();
				wr.EndList();
				wr.WriteString("p", "2");
			  wr.EndDictionary();			  
			  wr.WriteString("code", "Ok");
			wr.EndDictionary();

			string t = wr.GetString();
			Test.Assert(s == t);
		}

		static void ErrorMultiRoot()
		{
			JsonWriter wr = new JsonWriter();
			wr.WriteString(null, "abc");
			wr.WriteInt(null, 123);
		}

		static void ErrorRootNodeHasName()
		{
			// A root value node should not have a name
			JsonWriter wr = new JsonWriter();
			wr.WriteString("name", "abc");
		}

		static void ErrorListChildHasName()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.WriteString("name", "abc");
		}

		static void ErrorDictionaryChildWithoutName()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.WriteString(null, "abc");
		}

		static void ErrorDictionaryNotClosed()
		{
			// A dictionary is not closed before completing
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.GetString();
		}

		static void ErrorCloseNonexistDictionary()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.EndDictionary();
			wr.EndDictionary();
		}

		static void ErrorCloseNonexistList()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.EndList();
			wr.EndList();
		}

		static void ErrorCloseDictionaryByList()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginDictionary(null);
			wr.EndList();
		}

		static void ErrorCloseListByDictionary()
		{
			JsonWriter wr = new JsonWriter();
			wr.BeginList(null);
			wr.EndDictionary();
		}

		static void TestJsonWriteExpression()
		{
			JE expr = JE.New() + "abc";
			Test.Assert(expr.GetString() == "\"abc\"");

			expr = JE.New() + 123;
			Test.Assert(expr.GetString() == "123");

			expr = JE.New() + true;
			Test.Assert(expr.GetString() == "true");

			expr = JE.New() + JE.Null();
			Test.Assert(expr.GetString() == "null");

			expr = JE.New() +
				JE.Dict() +
					JE.Pair("name", "abc") +
					JE.Pair("value", 123) +
					JE.Pair("switch", false) +
					JE.Null("addon") +
				JE.EDict();
			Test.Assert(expr.GetString() == "{\"name\":\"abc\",\"value\":123,\"switch\":false,\"addon\":null}");

			expr = JE.New() +
				JE.List() +
					"abc" +
					123 +
				JE.EList();
			Test.Assert(expr.GetString() == "[\"abc\",123]");

			expr = JE.New() +
				JE.Dict() +
					JE.Pair("result", "ok") +
					JE.List("items") +
						JE.Dict() +
							JE.Pair("name", "abc") +
							JE.Pair("value", 123) +
						JE.EDict() +
						JE.Dict() +
							JE.Pair("name", "def") +
							JE.Pair("value", 456) +
						JE.EDict() +
					JE.EList() +
					JE.List("weight") + 
						"nil" +
						180 +
					JE.EList() +
				JE.EDict();
			string s =
				"{" +
				  "\"result\":\"ok\"," +
				  "\"items\":[" +
					"{\"name\":\"abc\",\"value\":123}," +
					"{\"name\":\"def\",\"value\":456}" +
				  "]," +
				  "\"weight\":[\"nil\",180]" +
				 "}";
			string t = expr.GetString();
			Test.Assert(s == t);
		}

		static void TestJsonWriteExpressionFormat()
		{
			JE expr = JE.New() +
				JE.Dict() +
					JE.Pair("name", "abc") +
					JE.Pair("value", 123) +
				JE.EDict();
			JsonExpressFormater jsf = new JsonExpressFormater();
			jsf.IndentChars = " ";
			jsf.NewLineChars = "\n";
			Test.Assert(expr.GetString(jsf) == "{\n \"name\":\"abc\",\n \"value\":123\n}");
		}

		static void TestJsonWriteExpressionNested()
		{
			// A dictionary sub-expression
			JE sub = JE.New() +
				JE.Dict() +
					JE.Pair("name", "abc") +
					JE.Pair("value", 123) +
				JE.EDict();
			JE expr = JE.New() + sub;
			Test.Assert(expr.GetString() == "{\"name\":\"abc\",\"value\":123}");

			// A dictionary sub-expression under a list
			expr = JE.New() +
				JE.List() +
					1 + 
					sub + 
					2 +
				JE.EList();
			Test.Assert(expr.GetString() == "[1,{\"name\":\"abc\",\"value\":123},2]");

			// A dictionary sub-expression under a dictionary
			expr = JE.New() +
				JE.Dict() +
					JE.Pair("result", "ok") +
					JE.Pair("body", sub) +
					JE.Pair("extra", 0) +
				JE.EDict();
			Test.Assert(expr.GetString() == "{\"result\":\"ok\",\"body\":{\"name\":\"abc\",\"value\":123},\"extra\":0}");

			// A list sub-expression
			sub = JE.New() +
				JE.List() +
					"abc" +
					123 +
				JE.EList();
			expr = JE.New() + sub;
			Test.Assert(expr.GetString() == "[\"abc\",123]");

			// A list sub-expression under a list
			expr = JE.New() +
				JE.List() + 
					1 + 
					sub + 
					2 +
				JE.EList();
			Test.Assert(expr.GetString() == "[1,[\"abc\",123],2]");

			// A list sub-expression under a dictionary
			expr = JE.New() +
				JE.Dict() +
					JE.Pair("result", "ok") +
					JE.Pair("body", sub) +
					JE.Pair("extra", 0) +
				JE.EDict();
			Test.Assert(expr.GetString() == "{\"result\":\"ok\",\"body\":[\"abc\",123],\"extra\":0}");
		}

		static void TestJsonWriteReopenDictionary()
		{
			// Original JE
			JE expr = JE.New() + 
				JE.Dict() + 
					JE.Pair("a", 1) +
				JE.EDict();
			JsonNode root = expr.Complete();	// the root dictionary node to be re-opened

			// Method 1, manually add a new JsonNode
			JsonNode node = new JsonNode(2);
			node.Name = "b";
			root.AddDictionaryItem(node);
			Test.Assert(JE.Set(root).GetString() == "{\"a\":1,\"b\":2}");

			// Method 2, using JsonWriter re-open function
			JsonWriter wr = new JsonWriter(root, true);	// Re-open root node to append items
			wr.WriteInt("c", 3);
			wr.EndDictionary();	// Must explicitly close the root collection re-opened
			Test.Assert(JE.Set(root).GetString() == "{\"a\":1,\"b\":2,\"c\":3}");

			// Method 3, using JE re-open function
			expr = 
				JE.Reopen(root) + 
					JE.Pair("d", 4) +
				JE.EDict();		// Must explicitly close the root collection re-opened
			Test.Assert(expr.GetString() == "{\"a\":1,\"b\":2,\"c\":3,\"d\":4}");
		}

		static void TestJsonWriteReopenList()
		{
			// Original JE
			JE expr = JE.New() + 
				JE.List() + 
					1 +
				JE.EList();
			JsonNode root = expr.Complete();	// the root list node to be re-opened

			// Method 1, manually add a new JsonNode
			JsonNode node = new JsonNode(2);
			root.AddListItem(node);
			Test.Assert(JE.Set(root).GetString() == "[1,2]");

			// Method 2, using JsonWriter re-open function
			JsonWriter wr = new JsonWriter(root, true);	// Re-open root node to append items
			wr.WriteInt(null, 3);
			wr.EndList();	// Must explicitly close the root collection re-opened
			Test.Assert(JE.Set(root).GetString() == "[1,2,3]");

			// Method 3, using JE re-open function
			expr = 
				JE.Reopen(root) + 
					4 +
				JE.EList();		// Must explicitly close the root collection re-opened
			Test.Assert(expr.GetString() == "[1,2,3,4]");
		}

		static void TestJsonWriteReopenComplex()
		{
			JE eMain = JE.New() +
				JE.Dict() +
					JE.List("keys") +
						1 +
					JE.EList() +
					JE.Dict("units") +
						JE.Pair("1", "local") +
					JE.EDict() +
				JE.EDict();		
			JsonNode root = eMain.Complete();

			// Add an item to List "keys"
			JsonNode nodeKeys = root["keys"];
			JE e = JE.Reopen(nodeKeys) + 2 + JE.EList();

			// Add an item to Dictionary "nodes"
			JsonNode nodeUnits = root["units"];
			e = JE.Reopen(nodeUnits) + JE.Pair("2", "remote") + JE.EDict();

			string t = eMain.GetString();
			Test.Assert(t == "{\"keys\":[1,2],\"units\":{\"1\":\"local\",\"2\":\"remote\"}}");
		}

		public static void Run()
		{
			Console.WriteLine("TestJson.TestJsonWriter");

			TestWriteValues();

			TestWriteDictionary();
			TestWriteDictionaryFormat();

			TestWriteList();
			TestWriteListFormat();

			TestWriteDictionaryUnderDictionary();
			TestWriteDictionaryUnderDictionaryFormat();

			TestWriteListUnderDictionary();
			TestWriteListUnderDictionaryFormat();

			TestWriteDictionaryUnderList();
			TestWriteDictionaryUnderListFormat();

			TestWriteListUnderList();
			TestWriteListUnderListFormat();

			TestComplextSample();

			Test.AssertExceptionType<LexicalException>(ErrorMultiRoot);
			Test.AssertExceptionType<LexicalException>(ErrorRootNodeHasName);
			Test.AssertExceptionType<LexicalException>(ErrorListChildHasName);
			Test.AssertExceptionType<LexicalException>(ErrorDictionaryChildWithoutName);
			Test.AssertExceptionType<LexicalException>(ErrorDictionaryNotClosed);
			Test.AssertExceptionType<LexicalException>(ErrorCloseNonexistDictionary);
			Test.AssertExceptionType<LexicalException>(ErrorCloseNonexistList);
			Test.AssertExceptionType<LexicalException>(ErrorCloseDictionaryByList);
			Test.AssertExceptionType<LexicalException>(ErrorCloseListByDictionary);

			TestJsonWriteExpression();
			TestJsonWriteExpressionFormat();
			TestJsonWriteExpressionNested();

			TestJsonWriteReopenDictionary();
			TestJsonWriteReopenList();
			TestJsonWriteReopenComplex();
		}
	}
}
