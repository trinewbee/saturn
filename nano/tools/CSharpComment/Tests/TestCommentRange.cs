using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.UnitTest;

namespace CSharpComment.Tests
{
	class TestCommentRange
	{
		CodeParser m_cp = new CodeParser();

		void testLineComment()
		{
			var text = @"  //first
  // 
  // third line  ";
			var rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 3);
			checkLineComment(text, rgs[0], "first");
			checkLineComment(text, rgs[1], "");
			checkLineComment(text, rgs[2], "third line");

			text = "\r\n\n\n\r\n";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 0);

			text = "// a // b\r\n";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 1);
			checkLineComment(text, rgs[0], "a // b");

			text = "// a /* b */\r\n";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 1);
			checkLineComment(text, rgs[0], "a /* b */");
		}

		void testLineCodeComment()
		{
			var text = @"  ///first
  /// 
  /// third line  ";
			var rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 3);
			checkLineCodeComment(text, rgs[0], "first");
			checkLineCodeComment(text, rgs[1], "");
			checkLineCodeComment(text, rgs[2], "third line");

			text = @"/// 1
  // 2
  /// 3
";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 3);
			checkLineCodeComment(text, rgs[0], "1");
			checkLineComment(text, rgs[1], "2");
			checkLineCodeComment(text, rgs[2], "3");
		}

		void testBlockComment()
		{
			var text = "/* a // b /* c */\r\n";
			var rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 1);
			checkBlockComment(text, rgs[0], "a // b /* c");

			text = @"
  /* This
   * go
   *//*next*//*
  */
";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 3);
			checkBlockComment(text, rgs[0], "This\r\n   * go");
			checkBlockComment(text, rgs[1], "next");
			checkBlockComment(text, rgs[2], "");

			text = @"/* // hello */// hello";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 2);
			checkBlockComment(text, rgs[0], "// hello");
			checkLineComment(text, rgs[1], "hello");
		}

		void testBlockCodeComment()
		{
			var text = "/** a // b /* c */\r\n";
			var rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 1);
			checkBlockCodeComment(text, rgs[0], "a // b /* c");

			text = "/** a /// b /** c */\r\n";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 1);
			checkBlockCodeComment(text, rgs[0], "a /// b /** c");

			text = @"
  /** This
    * go
    *//**next*//**
  */
";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 3);
			checkBlockCodeComment(text, rgs[0], "This\r\n    * go");
			checkBlockCodeComment(text, rgs[1], "next");
			checkBlockCodeComment(text, rgs[2], "");

			text = @"/** /// hello *//// hello";
			rgs = m_cp.ParseText(text);
			Test.Assert(rgs.Count == 2);
			checkBlockCodeComment(text, rgs[0], "/// hello");
			checkLineCodeComment(text, rgs[1], "hello");
		}

		void checkLineComment(string text, Range rg, string body)
		{
			Test.Assert(rg.Type == RangeType.LineComment);
			Test.Assert(rg.BodyBegin > rg.Begin && rg.BodyEnd >= rg.BodyBegin && rg.End >= rg.BodyEnd);				
			Test.Assert(text.Substring(rg.Begin, 2) == "//");
			Test.Assert(text.Substring(rg.BodyBegin, rg.BodyEnd - rg.BodyBegin) == body);
		}

		void checkLineCodeComment(string text, Range rg, string body)
		{
			Test.Assert(rg.Type == RangeType.LineCodeComment);
			Test.Assert(rg.BodyBegin > rg.Begin && rg.BodyEnd >= rg.BodyBegin && rg.End >= rg.BodyEnd);
			Test.Assert(text.Substring(rg.Begin, 3) == "///");
			Test.Assert(text.Substring(rg.BodyBegin, rg.BodyEnd - rg.BodyBegin) == body);
		}

		void checkBlockComment(string text, Range rg, string body)
		{
			Test.Assert(rg.Type == RangeType.BlockComment);
			Test.Assert(rg.BodyBegin > rg.Begin && rg.BodyEnd >= rg.BodyBegin && rg.End > rg.BodyEnd);
			Test.Assert(text.Substring(rg.Begin, 2) == "/*" && text.Substring(rg.End - 2, 2) == "*/");
			var body_t = text.Substring(rg.BodyBegin, rg.BodyEnd - rg.BodyBegin);
			Test.Assert(body_t == body);
		}

		void checkBlockCodeComment(string text, Range rg, string body)
		{
			Test.Assert(rg.Type == RangeType.BlockCodeComment);
			Test.Assert(rg.BodyBegin > rg.Begin && rg.BodyEnd >= rg.BodyBegin && rg.End > rg.BodyEnd);
			Test.Assert(text.Substring(rg.Begin, 3) == "/**" && text.Substring(rg.End - 2, 2) == "*/");
			var body_t = text.Substring(rg.BodyBegin, rg.BodyEnd - rg.BodyBegin);
			Test.Assert(body_t == body);
		}

		public static void Run()
		{
			Console.WriteLine("TestCommentRange");
			var o = new TestCommentRange();
			o.testLineComment();
			o.testLineCodeComment();
			o.testBlockComment();
			o.testBlockCodeComment();
		}
	}
}
