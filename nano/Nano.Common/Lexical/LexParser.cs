using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Nano.Lexical
{
	public class LexLine
	{
		public string Path; // File name
		public int LN;  // Line number
		public List<LexToken> Tokens;

		public LexLine(string path, int ln)
		{
			Path = path;
			LN = ln;
			Tokens = new List<LexToken>();
		}
	}

	public interface LexParseAccept
	{
		LexToken IsKeyword(string id);

		LexToken ParseSymbol(string text, ref int pos);
	}

	public class LexParser
	{
		#region Customization

		public delegate string TryParseEntityDelegate(string line, ref int pos);
		public TryParseEntityDelegate TryParseEntity = def_TryParseEntity;

		#endregion

		LexParseAccept m_acpt;

		public LexParser(LexParseAccept acpt) { m_acpt = acpt; }

		public void Parse(IEnumerable<string> lines, List<LexLine> lexLines, string path = "")
		{
			LexLine lexLine = null;
			int ln = 0;
			foreach (var line in lines)
			{
				int start;
				if (!IsContinueSign(line, out start))
				{
					lexLine = new LexLine(path, ++ln);
					lexLines.Add(lexLine);
				}
				ParseLine(line, lexLine.Tokens, start);
			}
		}

		public static IEnumerable<string> EnumTextLines(TextReader tr)
		{
			string line;
			while ((line = tr.ReadLine()) != null)
				yield return line;
		}

		public void Parse(TextReader tr, List<LexLine> lexLines, string path = "")
		{
			Parse(EnumTextLines(tr), lexLines, path);
		}

		public void Parse(string path, List<LexLine> lines)
		{
			using (TextReader tr = new StreamReader(path, Encoding.UTF8))
				Parse(tr, lines, path);
		}

		internal static bool IsContinueSign(string line, out int start)
		{
			start = 0;
			int pos = EatSpaces(line, 0);
			if (pos >= line.Length)
				return false;

			int pos2 = NextSpace(line, pos);
			if (pos2 - pos == 2 && line.Substring(pos, 2) == "--")
			{
				start = pos2;
				return true;
			}

			return false;
		}

		public void ParseLine(string line, List<LexToken> tokens, int start)
		{
			int pos = EatSpaces(line, start);
			while (pos < line.Length)
			{
				var token = ParseToken(line, ref pos);
				tokens.Add(token);
				pos = EatSpaces(line, pos);
			}
		}

		public LexToken ParseToken(string line, ref int pos)
		{
			char ch = line[pos];
			Debug.Assert(ch > ' ');
			int bpos = pos;
			LexToken token;
			if (ch >= '0' && ch <= '9')
				token = ParseNumberToken(line, ref pos);
			else if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_')
				token = ParseEntityToken(line, ref pos);
			else if (ch == '\"')
				token = ParseStringToken(line, ref pos);
			else
				token = m_acpt.ParseSymbol(line, ref pos);
			token.Pos = bpos;
			token.Len = pos - bpos;
			return token;
		}

		#region Parse literal token

		internal static LexToken ParseNumberToken(string line, ref int pos)
		{
			var bpos = pos;
			long value = ParseIntNumberToken(line, ref pos);
			char ch = pos < line.Length ? line[pos] : '\0';
			if (ch == '.')
			{
				double dblValue = ParseDblNumberToken(value, line, ref pos);
				ch = pos < line.Length ? line[pos] : '\0';
				if (ch == 'e' || ch == 'E')
					dblValue = ParseScienceNumberToken(dblValue, line, ref pos);
				return new LexToken(LexTokenType.Double) { VD = dblValue, VS = line.Substring(bpos, pos - bpos) };
			}
			else if (ch == 'e' || ch == 'E')
			{
				double dblValue = ParseScienceNumberToken(value, line, ref pos);
				return new LexToken(LexTokenType.Double) { VD = dblValue, VS = line.Substring(bpos, pos - bpos) };
			}
			else if (ch == 'l' || ch == 'L')
			{
				++pos;
				return new LexToken(LexTokenType.Long) { VL = value, VS = line.Substring(bpos, pos - bpos) };
			}
			else if (Math.Abs(value) >= 0x80000000L)
				return new LexToken(LexTokenType.Long) { VL = value, VS = line.Substring(bpos, pos - bpos) };
			else
				return new LexToken(LexTokenType.Int) { VI = (int)value, VS = line.Substring(bpos, pos - bpos) };
		}

		internal static long ParseIntNumberToken(string line, ref int pos)
		{
			Debug.Assert(line[pos] >= '0' && line[pos] <= '9');
			long value = 0;
			for (; pos < line.Length; ++pos)
			{
				char ch = line[pos];
				if (ch >= '0' && ch <= '9')
				{
					long newValue = value * 10 + (ch - '0');
					if (newValue < value)
						throw new OverflowException();
					value = newValue;
				}
				else
					break;
			}
			return value;
		}

		internal static double ParseDblNumberToken(double value, string line, ref int pos)
		{
			Debug.Assert(line[pos] == '.');
			double prec = 0.1, sub = 0.0;
			for (++pos; pos < line.Length; ++pos)
			{
				char ch = line[pos];
				if (ch >= '0' && ch <= '9')
				{
					sub = sub + (ch - '0') * prec;
					prec *= 0.1;
				}
				else
					break;
			}
			return value + sub;
		}

		internal static double ParseScienceNumberToken(double value, string line, ref int pos)
		{
			char ch = line[pos];
			Debug.Assert(ch == 'e' || ch == 'E');
			++pos;

			bool neg = false;
			ch = line[pos];
			if (ch == '-')
			{
				neg = true;
				++pos;
			}
			else if (ch == '+')
				++pos;

			if (pos >= line.Length || line[pos] < '0' || line[pos] > '9')
				throw new Exception("Exponent required");

			int exp = 0;
			for (; pos < line.Length; ++pos)
			{
				ch = line[pos];
				if (ch >= '0' && ch <= '9')
				{
					exp = exp * 10 + (ch - '0');
					if (exp > 308)
						throw new OverflowException();
				}
				else
					break;
			}
			if (neg)
				exp = -exp;

			return value * Math.Pow(10.0, exp);
		}

		#endregion

		#region Parse entity token

		internal LexToken ParseEntityToken(string line, ref int pos)
		{
			string entity = TryParseEntity(line, ref pos);
			LexToken token = m_acpt.IsKeyword(entity);
			if (token != null)
				return token;

			return new LexToken(LexTokenType.Ident) { VS = entity };
		}

		static string def_TryParseEntity(string line, ref int pos)
		{
			int bpos = pos;
			char ch = line[bpos];
			Debug.Assert((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_');
			for (++pos; pos < line.Length; ++pos)
			{
				ch = line[pos];
				if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_')
					;
				else
					break;
			}
			return line.Substring(bpos, pos - bpos);
		}

		#endregion

		#region Parse string token

		internal static LexToken ParseStringToken(string line, ref int pos)
		{
			Debug.Assert(line[pos] == '\"');
			StringBuilder sb = new StringBuilder();
			for (++pos; pos < line.Length; ++pos)
			{
				char ch = line[pos];
				if (ch == '\"')
				{
					++pos;
					break;
				}
				else if (ch == '\\')
				{
					ch = line[++pos];
					switch (ch)
					{
						case '\\':
							sb.Append('\\');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case '\'':
							sb.Append('\'');
							break;
						case '\"':
							sb.Append('\"');
							break;
						default:
							throw new Exception("Unknown convention char: " + ch);
					}
				}
				else
					sb.Append(ch);
			}
			return new LexToken(LexTokenType.String) { VS = sb.ToString() };
		}

		#endregion

		#region Tool kit methods

		// 从给定位置开始，找到第一个非空格字符
		public static int EatSpaces(string line, int pos)
		{
			for (; pos < line.Length; ++pos)
			{
				char ch = line[pos];
				if (ch > 32)
					break;
			}
			return pos;
		}

		// 从给定位置开始，找到第一个空格或非显示字符（编码小于空格的）
		public static int NextSpace(string line, int pos)
		{
			for (; pos < line.Length; ++pos)
			{
				char ch = line[pos];
				if (ch <= 32)
					break;
			}
			return pos;
		}

		public static string ParseString(string line, ref int pos)
		{
			var token = ParseStringToken(line, ref pos);
			if (token.Type != LexTokenType.String)
				throw new Exception("Not a string literal");
			return token.VS;
		}

		#endregion

		#region Factory methods

		public static List<LexToken> ParseSource(LexParseAccept acp, string text, int start = 0)
		{
			var tokens = new List<LexToken>();
			var lex = new LexParser(acp);

			int pos = LexParser.EatSpaces(text, start);
			while (pos < text.Length)
			{
				var token = lex.ParseToken(text, ref pos);
				tokens.Add(token);
				pos = LexParser.EatSpaces(text, pos);
			}
			return tokens;
		}

		#endregion
	}

	/// <summary>用于 CommonLexAccept 的符号接口</summary>
	public interface ISymbolFactory
	{
		int LineComment { get; }
		int BlockComment { get; }
		LexToken Get(string id);
	}

	/// <summary>用于 CommonLexAccept 的关键字接口</summary>
	public interface IKeywordFactory
	{
		LexToken Get(string id);
	}

	/// <summary>IKeywordFactory 的空实现</summary>
	public class NullKeywordFactory : IKeywordFactory
	{
		public LexToken Get(string id) => null;
	}

	/// <summary>用于类 C / C# 代码的通用词法解释回调类</summary>
	/// <remarks>
	/// 该类使用 ISymbolFactory 提供的符号表，和 IKeywordFactory 提供的关键字表。
	/// 支持单字符符号和双字符符号，并且总是优先匹配双字符符号。
	/// 支持 // 和 /* */ 风格的注释，因此符号表里必须提供对应的注释符号。
	/// </remarks>
	public class CommonLexAccept : LexParseAccept
	{
		ISymbolFactory m_symbols;
		IKeywordFactory m_keywords;

		public CommonLexAccept(ISymbolFactory symbols, IKeywordFactory keywords)
		{
			m_symbols = symbols;
			m_keywords = keywords;
		}

		public LexToken IsKeyword(string id) => m_keywords.Get(id);

		// 最大匹配，例如，连续两个 + 会匹配成 ++
		public LexToken ParseSymbol(string text, ref int pos)
		{
			if (pos >= text.Length)
				throw new IndexOutOfRangeException();

			var token = TryParseDoubleChar(text, ref pos);
			if (token != null)
				return token;

			string s = new string(text[pos], 1);
			token = m_symbols.Get(s);
			if (token == null)
				throw new ArgumentException("UnsupportedSymbol:" + s);

			++pos;
			return token;
		}

		LexToken TryParseDoubleChar(string text, ref int pos)
		{
			if (pos + 2 > text.Length)
				return null;

			string s = text.Substring(pos, 2);
			var token = m_symbols.Get(s);
			if (token == null)
				return null;

			if (token.VI == m_symbols.LineComment)
				return ParseLineComment(text, ref pos);
			else if (token.VI == m_symbols.BlockComment)
				return ParseBlockComment(text, ref pos);

			pos += 2;
			return token;
		}

		LexToken ParseLineComment(string text, ref int pos)
		{
			// Search for end of line
			int bpos = pos + 2;
			pos = text.IndexOf('\n', bpos);
			pos = pos >= 0 ? pos + 1 : text.Length;

			string str = text.Substring(bpos, pos - bpos).Trim();
			return new LexToken(LexTokenType.Symbol) { VI = m_symbols.LineComment, VS = str };
		}

		LexToken ParseBlockComment(string text, ref int pos)
		{
			int bpos = pos + 2;
			pos = text.IndexOf("*/", bpos);
			if (pos < 0)
				throw new Exception("End of comment not found");

			string str = text.Substring(bpos, pos - bpos).Trim();
			pos += 2;
			return new LexToken(LexTokenType.Symbol) { VI = m_symbols.BlockComment, VS = str };
		}
	}
}
