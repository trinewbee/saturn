using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Lexical;

namespace Nano.Ext.Marshal
{
	/// <summary>ODL 节点</summary>
	public class OdlNode
	{
		public string Name { get; }
		public Dictionary<string, string> Attributes { get; }
		public List<OdlNode> Children { get; }

		public OdlNode(string name)
		{
			Name = name;
			Attributes = new Dictionary<string, string>();
			Children = new List<OdlNode>();
		}

		public void AddNode(OdlNode node) => Children.Add(node);

		public override string ToString() => "Node " + Name;
	}

	enum OdlSymbols
	{
		None,
		Let,	// =
		Div,	// /
		LineComment,
		BlockComment,
		EOL
	}

	class OdlSymbolFactory : ISymbolFactory
	{
		public int LineComment => (int)OdlSymbols.LineComment;

		public int BlockComment => (int)OdlSymbols.BlockComment;

		public LexToken Get(string id)
		{
			switch (id)
			{
				case "=":
					return Make(OdlSymbols.Let, id);
				case "/":
					return Make(OdlSymbols.Div, id);
				default:
					return null;
			}
		}

		static LexToken Make(OdlSymbols sy, string id) => new LexToken(LexTokenType.Symbol) { VI = (int)sy, VS = id };
	}

	/// <summary>ODL 解析类</summary>
	public class OdlParser
	{
		const string TokenAfterEnd = "Unexpected token after end of node";

		public OdlNode Parse(IEnumerable<string> text)
		{
			var lines = LexParse(text);
			return ParseSyntax(lines);
		}

		/// <summary>解析 ODL 文档</summary>
		/// <param name="tr">文本流</param>
		/// <returns>返回文档的根节点</returns>
		public OdlNode Parse(TextReader tr)
		{
			var e = LexParser.EnumTextLines(tr);
			var lines = LexParse(e);
			return ParseSyntax(lines);
		}

		public OdlNode Parse(string path)
		{
			using (TextReader tr = new StreamReader(path, Encoding.UTF8))
				return Parse(tr);
		}

		OdlNode ParseSyntax(List<LexLine> lines)
		{
			OdlNode root = null;
			var stack = new Stack<OdlNode>();
			foreach (var line in lines)
			{
				SelectMeanToken(line.Tokens);
				if (line.Tokens.Count == 0)
					continue;

				var first = line.Tokens[0];
				if (first.Type == LexTokenType.Ident)
				{
					bool closed;
					var node = ParseNode(line, 0, out closed);
					if (root == null)
						root = node;
					else if (stack.Count == 0)
						throw new LexicalException(first.Pos, "Multiple root");
					else
						stack.Peek().AddNode(node);

					if (!closed)
						stack.Push(node);
				}
				else if (first.Type == LexTokenType.Symbol)
				{
					var sy = (OdlSymbols)first.VI;
					if (sy == OdlSymbols.Div)
						ParseEndNode(line, 0, stack);
					else
						throw new LexicalException(0, "Wrong symbol token");
				}
				else
					throw new LexicalException(0, "Wrong token");
			}

			if (root == null)
				throw new LexicalException(0, "No root node");
			else if (stack.Count != 0)
				throw new LexicalException(0, "Node not closed");
			return root;
		}

		#region Lexical

		List<LexLine> LexParse(IEnumerable<string> text)
		{
			var symbols = new OdlSymbolFactory();
			var keywords = new NullKeywordFactory();
			var accp = new CommonLexAccept(symbols, keywords);
			var parser = new LexParser(accp);
			var lines = new List<LexLine>();
			parser.Parse(text, lines);
			return lines;
		}

		static void SelectMeanToken(List<LexToken> tokens)
		{
			for (int i = tokens.Count - 1; i >= 0; --i)
			{
				var token = tokens[i];
				if (token.Type != LexTokenType.Symbol)
					continue;

				var sy = (OdlSymbols)token.VI;
				if (sy > OdlSymbols.None && sy < OdlSymbols.LineComment)
					continue;

				tokens.RemoveAt(i);
			}
		}

		#endregion

		#region Element

		OdlNode ParseNode(LexLine line, int pos, out bool closed)
		{
			closed = false;
			var tokens = line.Tokens;
			var token = NextToken(tokens, ref pos);
			Debug.Assert(token.Type == LexTokenType.Ident);
			var node = new OdlNode(token.VS);

			while (pos < tokens.Count)
			{
				token = PeekToken(tokens, pos);
				if (token.Type == LexTokenType.Ident)
				{
					var pair = ParseAttr(line, ref pos);
					node.Attributes.Add(pair.Key, pair.Value);
				}
				else if (token.Type == LexTokenType.Symbol)
				{
					var sy = (OdlSymbols)token.VI;
					if (sy == OdlSymbols.Div)
					{
						closed = true;
						if (++pos < tokens.Count)
							throw new LexicalException(tokens[pos].Pos, TokenAfterEnd);
					}
					else
						throw new LexicalException(token.Pos, "Wrong symbol token");
				}
				else
					throw new LexicalException(token.Pos, "Wrong token");
			}

			return node;
		}

		KeyValuePair<string, string> ParseAttr(LexLine line, ref int pos)
		{
			var tokens = line.Tokens;
			var token = NextToken(tokens, ref pos);
			Debug.Assert(token.Type == LexTokenType.Ident);
			var key = token.VS;

			token = PeekToken(tokens, pos);
			if (token == null || !CommonLexKit.IsSymbol(token, (int)OdlSymbols.Let))
				return new KeyValuePair<string, string>(key, null);
			++pos;

			token = NextToken(tokens, ref pos);
			if (token == null)
				throw new LexicalException(token.Pos, "Attribute value missing");

			switch (token.Type)
			{
				case LexTokenType.Ident:
				case LexTokenType.String:
				case LexTokenType.Int:
				case LexTokenType.Long:
				case LexTokenType.Double:
					return new KeyValuePair<string, string>(key, token.VS);
				default:
					throw new LexicalException(token.Pos, "Wrong attribute value");
			}
		}

		void ParseEndNode(LexLine line, int pos, Stack<OdlNode> stack)
		{
			var tokens = line.Tokens;
			var token = NextToken(tokens, ref pos);
			Debug.Assert(CommonLexKit.IsSymbol(token, (int)OdlSymbols.Div));

			token = NextToken(tokens, ref pos);
			if (token.Type != LexTokenType.Ident)
				throw new LexicalException(token.Pos, "Ident expected");

			if (stack.Count == 0)
				throw new LexicalException(token.Pos, "No node in stack");
			var node = stack.Pop();

			var id = token.VS;
			if (node.Name != id)
				throw new LexicalException(token.Pos, "Node name not match");

			if (pos < tokens.Count)
				throw new LexicalException(tokens[pos].Pos, TokenAfterEnd);
		}

		LexToken PeekToken(List<LexToken> tokens, int pos) => pos < tokens.Count ? tokens[pos] : null;

		LexToken NextToken(List<LexToken> tokens, ref int pos)
		{
			if (pos < tokens.Count)
				return tokens[pos++];
			return null;
		}

		#endregion
	}
}
