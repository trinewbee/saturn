using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CSharpComment
{
	public static class CodeParseKit
	{
		public static char PeekChar(string text, int pos, int end) => pos < end ? text[pos] : '\0';

		public static char NextChar(string text, ref int pos, int end)
		{
			if (pos >= end)
				return '\0';
			return text[pos++];
		}

		public static bool IsSpace(char ch)
		{
			if (ch <= 32)
			{
				Debug.Assert(ch == '\t' || ch == '\n' || ch == '\r' || ch == ' ');
				return true;
			}
			else
				return false;
		}

		public static bool IsLetter(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

		public static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

		public static bool IsIdentHead(char ch) => IsLetter(ch) || ch == '_';

		public static bool IsIdentNext(char ch) => IsLetter(ch) || IsDigit(ch) || ch == '_';

		// 返回从 pos 开始，向后搜索到 end 之前的第一个非空字符
		// 如果到 end 之前没有非空字符，则返回 end
		public static int EatSpaces(string text, int pos, int end)
		{
			while (pos < end)
			{
				if (!IsSpace(text[pos]))
					return pos;
				++pos;
			}
			return pos;
		}

		// 返回从 pos 开始，向前搜索到 begin 为止的第一个非空字符
		// 如果到 start 都不是非空字符，则返回 start - 1
		public static int ReverseEatSpaces(string text, int pos, int begin)
		{
			while (pos >= begin)
			{
				if (!IsSpace(text[pos]))
					return pos;
				--pos;
			}
			return pos;
		}

		public static int ReadEndOfLine(string text, int pos, int end)
		{
			while (pos < end)
			{
				char ch = text[pos++];
				if (ch == '\n')
					return pos;
			}
			return end;
		}
	}

	enum RangeType
	{
		LineComment,
		BlockComment,
		LineCodeComment,
		BlockCodeComment,
		CodeBlock,
		String,
		Parenthese,
		Index
	}

	class Range
	{
		public RangeType Type;
		public int Begin, End;
		public int BodyBegin, BodyEnd;
	}

	class CodeParser
	{
		public List<Range> ParseFile(string path)
		{
			string text;
			using (var tr = new StreamReader(path, Encoding.UTF8))
				text = tr.ReadToEnd();
			return ParseText(text);
		}

		public List<Range> ParseText(string text)
		{
			int len = text.Length;
			var rgs = new List<Range>();
			int pos = CodeParseKit.EatSpaces(text, 0, len);
			while (pos < text.Length)
			{
				var rg = ReadRange(text, ref pos, len);
				rgs.Add(rg);
				Debug.Assert(rg.End == pos);
				pos = CodeParseKit.EatSpaces(text, pos, text.Length);
			}
			return rgs;
		}

		Range ReadRange(string text, ref int pos, int end)
		{
			Debug.Assert(!CodeParseKit.IsSpace(text[pos]));
			char ch = text[pos];
			if (ch == '/')
				return ReadCommentRange(text, ref pos, end);
			else if (CodeParseKit.IsLetter(ch))
				return ReadCodeRange(text, pos, end);
			else
				throw new ArgumentException("Unknown range header: " + ch);
		}

		#region Comments

		Range ReadCommentRange(string text, ref int pos, int end)
		{
			Debug.Assert(text[pos] == '/');
			int begin = pos++;
			char ch = CodeParseKit.NextChar(text, ref pos, end);
			if (ch == '/')
			{
				ch = CodeParseKit.PeekChar(text, pos, end);
				bool isCM = ch == '/';
				RangeType rgt = isCM ? RangeType.LineCodeComment : RangeType.LineComment;
				if (isCM)
					++pos;
				return ReadLineComment(rgt, text, begin, ref pos, end);
			}
			else if (ch == '*')
			{
				ch = CodeParseKit.PeekChar(text, pos, end);
				bool isCM = ch == '*';
				RangeType rgt = isCM ? RangeType.BlockCodeComment : RangeType.BlockComment;
				if (isCM)
					++pos;
				return ReadBlockComment(rgt, text, begin, ref pos, end);
			}
			else
				throw new ArgumentException("Unknown comment header: " + text.Substring(begin, 2));
		}

		Range ReadLineComment(RangeType type, string text, int begin, ref int pos, int end)
		{
			var rg = new Range { Type = type, Begin = begin };
			rg.BodyBegin = CodeParseKit.EatSpaces(text, pos, end);
			rg.End = CodeParseKit.ReadEndOfLine(text, pos, end);
			rg.BodyEnd = CodeParseKit.ReverseEatSpaces(text, rg.End - 1, rg.BodyBegin) + 1;
			if (rg.BodyEnd <= rg.BodyBegin)
				rg.BodyBegin = rg.BodyEnd = pos;  // no body
			pos = rg.End;
			return rg;
		}

		Range ReadBlockComment(RangeType type, string text, int begin, ref int pos, int end)
		{
			var rg = new Range { Type = type, Begin = begin };
			rg.BodyBegin = CodeParseKit.EatSpaces(text, pos, end);
			rg.End = text.IndexOf("*/", rg.BodyBegin, end - rg.BodyBegin);
			if (rg.End < 0)
				throw new ArgumentException("End of comment (\"*/\" symbols) not found");
			rg.BodyEnd = CodeParseKit.ReverseEatSpaces(text, rg.End - 1, rg.BodyBegin) + 1;
			if (rg.BodyEnd <= rg.BodyBegin)
				rg.BodyBegin = rg.BodyEnd = pos;  // no body
			pos = rg.End += 2;
			return rg;
		}

		#endregion

		static char[] TERMS = new char[] { ';', '(', '[', '{', '\"', '\'' };

		Range ReadCodeRange(string text, int pos, int len)
		{
			var rg = new Range { Type = RangeType.CodeBlock, Begin = pos };

			Debug.Assert(CodeParseKit.IsLetter(text[pos]));
			int epos = text.IndexOfAny(TERMS, pos);
			if (epos < 0)
				throw new ArgumentException("No terminate char found");

			return rg;
		}
	}
}
