using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace TestExt
{
	class TestOdlParser
	{
		OdlParser m_parser = new OdlParser();

		void testSimple()
		{
			var lines = new string[] {
				"a", " b1", "  c", "  /c", " /b1", " b2", " /b2", "/a"
			};
			var root = m_parser.Parse(lines);
			Test.Assert(root.Name == "a");
			var nodes = root.Children;
			Test.Assert(nodes.Count == 2 && nodes[0].Name == "b1" && nodes[1].Name == "b2");

			lines = new string[] { "a", " b1 /", " b2/", "/a" };
			root = m_parser.Parse(lines);
			nodes = root.Children;
			Test.Assert(nodes.Count == 2 && nodes[0].Name == "b1" && nodes[0].Children.Count == 0);
			Test.Assert(nodes[1].Name == "b2" && nodes[1].Children.Count == 0);

			lines = new string[] { "test/" };
			root = m_parser.Parse(lines);
			Test.Assert(root.Name == "test" && root.Children.Count == 0);
		}

		void testAttrs()
		{
			var lines = new string[] { "a name=test code=007 value=\"Hello world\"", "/a" };
			var root = m_parser.Parse(lines);
			var attrs = root.Attributes;
			Test.Assert(attrs.Count == 3 && attrs["name"] == "test" && attrs["code"] == "007" && attrs["value"] == "Hello world");

			lines = new string[] { "a v1=123L v2=9876543210 /" };
			root = m_parser.Parse(lines);
			attrs = root.Attributes;
			Test.Assert(attrs.Count == 2 && attrs["v1"] == "123L" && attrs["v2"] == "9876543210");

			lines = new string[] { "a v=\"测试\" t /" };
			root = m_parser.Parse(lines);
			attrs = root.Attributes;
			Test.Assert(attrs.Count == 2 && attrs["v"] == "测试" && attrs["t"] == null);
		}

		void testMultiLines()
		{
			var lines = new string[] { "a -", "-", "name=test /" };
			var root = m_parser.Parse(lines);
			Test.Assert(root.Name == "a");
			var attrs = root.Attributes;
			Test.Assert(attrs.Count == 1 && attrs["name"] == "test");

			lines = new string[]
			{
				"person name=Zhang", "props rank=1 /",
				"@desc", "Line 1", "Line 2", "/@desc",
				"/person"
			};
			root = m_parser.Parse(lines);
			Test.Assert(root.Name == "person" && root.Children.Count == 1);
			attrs = root.Attributes;
			Test.Assert(attrs["name"] == "Zhang" && attrs["desc"] == "Line 1\nLine 2");
			var node = root.Children[0];
			Test.Assert(node.Name == "props" && node.Attributes["rank"] == "1");
		}

		public static void Run()
		{
			Console.WriteLine($"TestMarshal.{nameof(TestOdlParser)}");
			var o = new TestOdlParser();
			o.testSimple();
			o.testAttrs();
			o.testMultiLines();
		}
	}
}
