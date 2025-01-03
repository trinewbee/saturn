﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Lexical;
using Nano.Nuts;

namespace Nano.Ext.Marshal
{
	/// <summary>ODL 节点</summary>
	public class OdlNode
	{
		public string Name { get; }
		public Dictionary<string, string> Attributes { get; }
		public List<string> AttrNames { get; }
		public List<OdlNode> Children { get; }
		public string this[string key] => Attributes[key];

		public OdlNode(string name)
		{
			Name = name;
			Attributes = new Dictionary<string, string>();
            AttrNames = new List<string>();
            Children = new List<OdlNode>();
		}

		public bool HasAttr(string key) => Attributes.ContainsKey(key);

		public string GetAttr(string key)
		{
			string value;
			if (Attributes.TryGetValue(key, out value))
				return value;
			return null;
		}

        public void AddNode(OdlNode node) => Children.Add(node);

        internal void AddAttr(string key, string value)
        {
            Attributes.Add(key, value);
            AttrNames.Add(key);
        }

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
		OdlNode m_root = null;
		Stack<OdlNode> m_stack = new Stack<OdlNode>();

		public OdlNode Parse(IEnumerable<string> lines)
		{
			if (m_stack.Count != 0)
				throw new Exception("MethodNotCompleted");

			m_root = null;
			string lastLine = null;
			var elines = lines.GetEnumerator();
			while (elines.MoveNext())
			{
				var line = elines.Current;
				var pos = LastNonSpace(line);

				// 处理续行符号
				if (pos >= 1 && line[pos] == '-' && line[pos - 1] == '-' && (pos < 2 || line[pos - 2] <= ' '))
				{
					// 本行末尾有续行符号
					line = line.Substring(0, pos - 1); // 去掉末尾的 -- 号
					lastLine = lastLine != null ? lastLine + line : line;
					continue;
				}

				line = lastLine != null ? lastLine + ' ' + line : line;
				lastLine = null;

				pos = LexParser.EatSpaces(line, 0);
				if (pos >= line.Length) // 空行
					continue;

				var ch = line[pos];
				if (ch == '#') // 注释
					continue;
				else if (ch == '@') // 多行属性
					ParseMultiLineAttr(line, pos, elines);
				else if (ch == '/') // 关闭标签
					ParseEndElementLine(line, pos);
				else
					ParseElementLine(line, pos);
			}
			G.Verify(lastLine == null, "LinesNotEnded");

			G.Verify(m_root != null, "NoContent");
			return m_root;
		}

		public OdlNode Parse(TextReader tr) => Parse(_FromTextReader(tr));

		static IEnumerable<string> _FromTextReader(TextReader tr)
		{
			string line;
			while ((line = tr.ReadLine()) != null)
				yield return line;
		}

		public OdlNode ParseFile(string path)
		{
			using (var tr = new StreamReader(path, Encoding.UTF8))
				return Parse(tr);
		}

		void ParseEndElementLine(string s, int pos)
		{
			Debug.Assert(s[pos] == '/');

			++pos;
			var ident = ParseIdent(s, ref pos);
			G.Verify(m_stack.Count != 0, "NoMatchingNode");

			var parent = m_stack.Pop();
			G.Verify(parent.Name == ident, "NodeNotMatch");

			ExamContentAfterClosingSymbol(s, pos);
		}

		void ParseElementLine(string s, int pos)
		{
			var symbol = ParseIdent(s, ref pos);
			var node = new OdlNode(symbol);
			if (m_stack.Count == 0)
			{
				G.Verify(m_root == null, "MultipleRootNode");
				m_root = node;
			}
			else
				m_stack.Peek().AddNode(node);

			var ch = pos < s.Length ? s[pos] : '\0';
			G.Verify(ch <= ' ' || ch == '/' || ch == '#', "WrongSymbolAfterNode");

			if (!ParseAttrs(node, s, pos))
				m_stack.Push(node);
		}

		// return true if ended with a closing symbol /
		bool ParseAttrs(OdlNode node, string s, int pos)
		{
			while ((pos = LexParser.EatSpaces(s, pos)) < s.Length)
			{
				var ch = s[pos];
				if (ch == '/') // closing
				{
					ExamContentAfterClosingSymbol(s, pos);
					return true;
				}
				else if (ch == '@') // Whole line element
					return ParseWholeLineAttr(node, s, pos);
                else if (ch == '#') // comments
                    return false;

                var key = ParseIdent(s, ref pos);
				ch = pos < s.Length ? s[pos] : (char)0;
				if (ch == '=')
				{
					++pos;
					ch = pos < s.Length ? s[pos] : (char)0;

					string value;
					if (ch <= ' ')
						value = "";
					else if (ch == '"')
					{
						value = Nano.Json.JsonParser.ParseStringValueToken(s, ref pos);
					}
					else
					{
						var epos = LexParser.NextSpace(s, pos);
						value = s.Substring(pos, epos - pos);
						G.Verify(value.Length == 0 || value[value.Length - 1] != '/', "WrongSymbolInAttrValue");
						pos = epos;
					}
					node.AddAttr(key, value);
				}
				else if (ch == '/')
				{
					node.AddAttr(key, null);
					ExamContentAfterClosingSymbol(s, pos);
					return true;
				}
				else if (ch <= ' ')
					node.AddAttr(key, "");
				else
					throw new NutsException("WrongSymbolAfterAttrKey");
			}
			return false;
		}

        // 解析元素行的整行属性
        bool ParseWholeLineAttr(OdlNode node, string s, int pos)
		{
			G.Verify(s[pos] == '@', "AtSymbolRequired");
            ++pos;
            var ident = ParseIdent(s, ref pos);
			G.Verify(s[pos] == ':', "CommaSymbolRequired");
			pos = LexParser.EatSpaces(s, pos + 1);

			// 处理元素行的整行属性，末尾包含 / 关闭符号的特殊场景 (/ 前面必须有至少一个空格，否则被视为属性值的一部分)
			// 独立的整行属性，末尾 / 仍然被视为属性值的一部分
            var pos2 = LastNonSpace(s);
			if (pos2 >= pos && pos2 < s.Length && s[pos2] == '/' && s[pos2 - 1] <= 32)
			{
				var str = s.Substring(pos, pos2 - pos - 1).TrimEnd();
                node.AddAttr(ident, str);
				return true;
            }
			else
			{
                var str = s.Substring(pos).TrimEnd();
                node.AddAttr(ident, str);
				return false;
            }
        }

		// 解析多行属性或独立整行属性
        void ParseMultiLineAttr(string s, int pos, IEnumerator<string> elines)
		{
            G.Verify(s[pos] == '@', "AtSymbolRequired");
            G.Verify(m_stack.Count != 0, "NoMatchingNode");

			++pos;
			var ident = ParseIdent(s, ref pos);
            pos = LexParser.EatSpaces(s, pos);
			var ch = PeekChar(s, pos);
			if (ch == ':')
			{
				// 整行属性
				pos = LexParser.EatSpaces(s, pos + 1);
				var str = s.Substring(pos).TrimEnd();
				var node = m_stack.Peek();
				node.AddAttr(ident, str);
				return;
			}
			else if (ch != '\0' && ch != '#')
				throw new NutsException("ContentsAfterCloseSymbol");            

			var sb = new StringBuilder();
			while (elines.MoveNext())
			{
				s = elines.Current;
				pos = LexParser.EatSpaces(s, 0);
				if (pos <= s.Length - ident.Length - 2 && s[pos] == '/')
				{
					if (s[pos + 1] == '@' && s.Substring(pos + 2, ident.Length) == ident)
					{
						// end of multi-line attribute
						var node = m_stack.Peek();
						node.AddAttr(ident, sb.ToString());
						return;
					}
				}

				if (sb.Length != 0)
					sb.Append('\n');
				sb.Append(s);
			}

			throw new NutsException("AttrNotCompleted");
		}		

		static void ExamContentAfterClosingSymbol(string s, int pos)
		{
			pos = LexParser.EatSpaces(s, pos + 1);
			G.Verify(pos >= s.Length || s[pos] == '#', "ContentsAfterCloseSymbol");
		}

        public static char PeekChar(string s, int pos) => pos < s.Length ? s[pos] : '\0';

        public static string ParseIdent(string s, ref int pos)
		{
			var ch = s[pos];
			G.Verify(IsIdentHead(ch), "InvalidIdentChar");

			string ident = new string(ch, 1);
			for (++pos; pos < s.Length; ++pos)
			{
				ch = s[pos];
				if (IsIdentChar(ch))
					ident += ch;
				else
					break;
			}

			return ident;
		}

		public static bool IsIdentHead(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';

		public static bool IsIdentChar(char ch) =>
			(ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
			ch == '-' || ch == '_' || ch == '.';

		public static int LastNonSpace(string s, int pos = -1)
		{
			if (pos < 0)
				pos = s.Length - 1;
			for (; pos >= 0; --pos)
			{
				if (s[pos] > ' ')
					return pos;
			}
			return -1;
		}
	}
}
