﻿using System;
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
            Test.Assert(root.HasAttr("name") && root.GetAttr("name") == "test");
            Test.Assert(!root.HasAttr("nam") && root.GetAttr("nam") == null);


            lines = new string[] { "a v1=123L v2=9876543210 /" };
			root = m_parser.Parse(lines);
			attrs = root.Attributes;
			Test.Assert(attrs.Count == 2 && attrs["v1"] == "123L" && attrs["v2"] == "9876543210");

			lines = new string[] { "a v=\"测试\" t x= /" };
			root = m_parser.Parse(lines);
			attrs = root.Attributes;
			Test.Assert(attrs.Count == 3 && attrs["v"] == "测试" && attrs["t"] == "" && attrs["x"] == "");
		}

		void testMultiLines()
		{
			var lines = new string[] { "a --", "--", "name=test /" };
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

			// 多行属性属性值前后的空格，都不会被去掉
			lines = new string[] { "a", "@c", "  x  ", "/@c", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "  x  ");
        }

		void testWholeLineAttr()
		{
            var lines = new string[] { "p name=Zhang @desc: male 29", "/p" };
            var root = m_parser.Parse(lines);
            Test.Assert(root.Name == "p");
            var attrs = root.Attributes;
            Test.Assert(attrs.Count == 2 && attrs["name"] == "Zhang" && attrs["desc"] == "male 29");

			lines = new string[] { 
				"p name=Wang @desc: male 29", 
				"@profile: xxx ## yyy /", 
				"@edu", "2001", "2005", "/@edu",
				"/p"
			};
            root = m_parser.Parse(lines);
            attrs = root.Attributes;
            Test.Assert(attrs.Count == 4 && attrs["name"] == "Wang" && attrs["desc"] == "male 29");
			Test.Assert(attrs["profile"] == "xxx ## yyy /" && attrs["edu"] == "2001\n2005");

			// 元素行的整行属性，后面可以接元素关闭符号 /
			lines = new string[] { "a @c: x /" };
            root = m_parser.Parse(lines);
			Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x");

			// 元素行的整行属性，末尾的 / 前面如果没有空格，被视为属性值的一部分，不是元素关闭符号
			lines = new string[] { "a @c: x/", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x/");

			// 非元素行的整行属性，末尾的 / 均不视为元素关闭符号
			lines = new string[] { "a", "@c: x /", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x /");

            // 整行属性的属性值前后的空格，都会被自动清除
            lines = new string[] { "a @c: \t x \t ", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x");

            // 整行属性的属性值前后的空格，都会被自动清除
            lines = new string[] { "a @c: \t x \t /" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x");

			// 整行属性的属性值前后的空格，都会被自动清除
			lines = new string[] { "a", "@c: \t x \t ", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root.Attributes["c"] == "x");
        }

        void testComments()
		{
			var lines = new string[] { "a # Line 1", "b / # Line 2", "# Line 3", "/a # Line 4" };
            var root = m_parser.Parse(lines);
			Test.Assert(root.Name == "a" && root.Attributes.Count == 0 && root.Children.Count == 1);

			lines = new string[] { "a b=#c #", "/a" };
            root = m_parser.Parse(lines);
			Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root["b"] == "#c");

			lines = new string[] { "a @c: x # y", "/a" };
            root = m_parser.Parse(lines);
            Test.Assert(root.Name == "a" && root.Attributes.Count == 1 && root["c"] == "x # y");
        }

		public static void Run()
		{
			Console.WriteLine($"TestMarshal.{nameof(TestOdlParser)}");
			var o = new TestOdlParser();
			o.testSimple();
			o.testAttrs();
			o.testMultiLines();
			o.testWholeLineAttr();
            o.testComments();
        }
	}
}
