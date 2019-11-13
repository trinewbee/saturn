using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Lexical
{
	/// <summary>表示词法解析过程中的异常</summary>
	public class LexicalException : Exception
	{
		/// <summary>获取错误发生的位置</summary>
		public int Position { get; }

		/// <summary>创建一个 LexicalException 实例</summary>
		/// <param name="code">错误代码</param>
		/// <param name="pos">错误发生的位置</param>
		/// <param name="message">错误说明</param>
		public LexicalException(int pos, string message) : base(message)
		{
			Position = pos;
		}
	}

	public enum LexTokenType
	{
		Symbol,     // operators
		String,
		Int,
		Long,
		Double,
		Keyword,    // keywords
		Ident,      // identifier
	}

	public class LexToken
	{
		public LexTokenType Type;
		public int VI;  // int literal, application defined ids
		public long VL; // long literal
		public double VD;   // double literal
		public string VS;   // string literal, keywords, identifiers, symbols
		public int N;   // indicating number of arguments, used in tuple, function, index, etc
		public int Pos, Len;    // Range in code line

		public LexToken(LexTokenType type)
		{
			Type = type;
		}
	}

	public static class CommonLexKit
	{
		public static bool IsIdent(LexToken token, string ident) => token.Type == LexTokenType.Ident && token.VS == ident;

		public static bool IsSymbol(LexToken token, int symbol) => token.Type == LexTokenType.Symbol && token.VI == symbol;

		public static bool IsKeyword(LexToken token, int keyword) => token.Type == LexTokenType.Keyword && token.VI == keyword;

		public static LexToken PeekNextMeanToken(IList<LexToken> tokens, ref int pos, Predicate<LexToken> pred, int end = -1)
		{
			if (end == -1)
				end = tokens.Count;
			for (; pos < end; ++pos)
			{
				var token = tokens[pos];
				if (pred(token))
					return token;
			}
			return null;
		}

		public static LexToken FetchNextMeanToken(IList<LexToken> tokens, ref int pos, Predicate<LexToken> pred, int end = -1)
		{
			var token = PeekNextMeanToken(tokens, ref pos, pred, end);
			if (token != null)
				++pos;
			return token;
		}
	}
}
