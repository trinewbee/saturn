using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Nano.Collection;
using Nano.Lexical;

namespace Nano.Json
{
	public enum JsonNodeType
	{
		Null,			// null
		String,			// string value
		Integer,		// 64-bit integer value
		Float,			// 64-bit IEEE floating-point value
		Boolean,		// boolean value
		Dictionary,		// a key-node dictionary
		NodeList,		// a node list
	}

	public class JsonNode
	{
		JsonNodeType m_type;
		string m_name = null;
		object m_value;

		#region Constructors

		public JsonNode(string value)
		{
			m_type = JsonNodeType.String;
			m_value = value;
		}

		public JsonNode(long value)
		{
			m_type = JsonNodeType.Integer;
			m_value = value;
		}

		public JsonNode(double value)
		{
			m_type = JsonNodeType.Float;
			m_value = value;
		}

		public JsonNode(bool value)
		{
			m_type = JsonNodeType.Boolean;
			m_value = value;
		}

		/// <summary>构建一个 Null，List 或 Dictionary Node</summary>
		/// <param name="type">Node 类型</param>
		/// <remarks>只能传入 Null，List 或 Dictionary，其他类型会导致 JsonException 异常</remarks>
		public JsonNode(JsonNodeType type)
		{
			m_type = type;
			switch (type)
			{
				case JsonNodeType.Dictionary:
					m_value = new Dictionary<string, JsonNode>();
					break;
				case JsonNodeType.NodeList:
					m_value = new List<JsonNode>();
					break;
				case JsonNodeType.Null:
					m_value = null;
					break;
				default:
					throw new LexicalException(0, "Unsupported node type");
			}
		}

		/// <summary>根据传入的 node 构建一个副本</summary>
		/// <param name="node">源 node</param>
		/// <param name="deepCopy">是否深度复制</param>
		/// <remarks>
		/// 对于值类型节点，会创建一个独立的副本。
		/// 对于 List 和 Dictionary，则要参考 deepCopy 参数。
		/// deepCopy 为 true 时，容器中的每个元素，包括递归子元素，都会被创建副本，因此返回一个以 node 为根的子树的完整副本。
		/// deepCopy 为 false 时，容器中的元素没有创建副本。
		/// 如果源 node 有 Name，则本方法也会将 Name 复制。
		/// </remarks>
		public JsonNode(JsonNode node, bool deepCopy = false)
		{
			m_type = node.m_type;
			m_name = node.m_name;
			if (m_type == JsonNodeType.NodeList)
			{
				var source = (List<JsonNode>)node.m_value;
				if (deepCopy)
					m_value = CollectionKit.Transform(source, x => new JsonNode(x, true));
				else
					m_value = new List<JsonNode>(source);
			}
			else if (m_type == JsonNodeType.Dictionary)
			{
				var source = (Dictionary<string, JsonNode>)node.m_value;
				if (deepCopy)
				{
					var dict = new Dictionary<string, JsonNode>(source.Count);
					foreach (var pair in source)
						dict.Add(pair.Key, new JsonNode(pair.Value, true));
					m_value = dict;
				}
				else
					m_value = new Dictionary<string, JsonNode>(source);
			}
			else
				m_value = node.m_value;
		}

		#endregion

		#region Properties

		public JsonNodeType NodeType
		{
			get { return m_type; }
		}

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		public object Value
		{
			get { return m_value; }
		}

		public string TextValue
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.String);
				return (string)m_value;
			}
		}

		public long IntValue
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.Integer);
				return (long)m_value;
			}
		}

		public double FloatValue
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.Float);
				return (double)m_value;
			}
		}

		public bool BoolValue
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.Boolean);
				return (bool)m_value;
			}
		}

		List<JsonNode> NodeList
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.NodeList);
				return (List<JsonNode>)m_value;
			}
		}

		Dictionary<string, JsonNode> NodeDictionary
		{
			get
			{
				Debug.Assert(m_type == JsonNodeType.Dictionary);
				return (Dictionary<string, JsonNode>)m_value;
			}
		}

		public int ChildCount
		{
			get
			{
				if (NodeType == JsonNodeType.Dictionary)
					return NodeDictionary.Count;
				else if (NodeType == JsonNodeType.NodeList)
					return NodeList.Count;
				else
					throw new LexicalException(0, "Specified node is not a collection");
			}
		}

		public JsonNode this[int index]
		{
			get
			{
				return NodeList[index];
			}
		}

		public JsonNode this[string key]
		{
			get
			{
				return NodeDictionary[key];
			}
		}

		public IEnumerable<JsonNode> ChildNodes
		{
			get
			{
				if (NodeType == JsonNodeType.Dictionary)
					return NodeDictionary.Values;
				else if (NodeType == JsonNodeType.NodeList)
					return NodeList;
				else
					throw new LexicalException(0, "Specified node is not enumerable");
			}
		}

		public bool IsNull
		{
			get { return NodeType == JsonNodeType.Null; }
		}

		public static implicit operator string(JsonNode node)
		{
			return node.TextValue;
		}

		public static implicit operator long(JsonNode node)
		{
			return node.IntValue;
		}

		public static implicit operator double(JsonNode node)
		{
			return node.FloatValue;
		}

		public static explicit operator float(JsonNode node)
		{
			return (float)node.FloatValue;
		}

		public static implicit operator bool(JsonNode node)
		{
			return node.BoolValue;
		}

		#endregion

		// Return null if no child with specified key found
		public JsonNode GetChildItem(string key)
		{
			JsonNode node;
			if (NodeDictionary.TryGetValue(key, out node))
				return node;
			else
				return null;
		}

		[Obsolete("Use AddChildItem instead")]
		public void AddDictionaryItem(JsonNode node)
		{
			if (node.Name == null)
				throw new LexicalException(0, "Node must have a name");

			NodeDictionary.Add(node.Name, node);
		}

		[Obsolete("Use AddChildItem instead")]
		public void AddListItem(JsonNode node)
		{
			if (node.Name != null)
				throw new LexicalException(0, "Node must not have a name");

			NodeList.Add(node);
		}

		public void AddChildItem(JsonNode node)
		{
			if (NodeType == JsonNodeType.Dictionary)
				AddDictionaryItem(node);
			else if (NodeType == JsonNodeType.NodeList)
				AddListItem(node);
			else
				throw new LexicalException(0, "Not a collection node");
		}

		public void DeleteChildItem(int index)
		{
			NodeList.RemoveAt(index);
		}

		public void DeleteChildItem(string key)
		{
			NodeDictionary.Remove(key);
		}

		public object TryGetChildValue(string key)
		{
			JsonNode node = NodeDictionary[key];
			return node != null ? node.Value : null;
		}

		public object TryGetChildValue(int index)
		{
			JsonNode node = NodeList[index];
			return node != null ? node.Value : null;
		}

		#region Walk children

		public void WalkChildren<T>(Action<T> accp)
		{
			foreach (JsonNode nodeItem in ChildNodes)
				accp((T)nodeItem.Value);
		}

		public void WalkChildren<T>(ICollection<T> values)
		{
			foreach (JsonNode nodeItem in ChildNodes)
				values.Add((T)nodeItem.Value);
		}

		#endregion
	}

	public static class JsonParser
	{
		public static JsonNode ParseText(string text)
		{
			int pos = 0;
			JsonNode root = ParseInnerTextVariant(text, ref pos);

			if (pos >= 0 && pos < text.Length)
			{
				pos = SeekNextNotSpace(text, pos);
				if (pos >= 0)
					throw new LexicalException(pos, "Unexpected content");
			}
			return root;
		}

		static JsonNode ParseInnerTextVariant(string text, ref int pos)
		{
			pos = SeekNextNotSpace(text, pos);
			if (pos < 0)
				throw new LexicalException(pos, "Expect content");

			char ch = text[pos];
			if (ch == '{')
				return ParseInnerTextDictionary(text, ref pos);
			else if (ch == '[')
				return ParseInnerTextList(text, ref pos);
			else if (ch == '\"')
				return ParseInnerTextStringValue(text, ref pos);
			else if ((ch >= '0' && ch <= '9') || (ch == '+' || ch == '-'))
				return ParseInnerTextNumberValue(text, ref pos);
			else if (ch == 'T' || ch == 't' || ch == 'F' || ch == 'f')
				return ParseInnerTextBooleanValue(text, ref pos);
			else if (ch == 'N' || ch == 'n')
				return ParseInnerTextNoneValue(text, ref pos);
			else
				throw new LexicalException(pos, "Unexpected symbol");
		}

		static JsonNode ParseInnerTextStringValue(string text, ref int pos)
		{
			Debug.Assert(text[pos] == '\"');
			
			string value = ParseStringValueToken(text, ref pos);
			return new JsonNode(value);
		}

		static JsonNode ParseInnerTextNumberValue(string text, ref int pos)
		{
#if false
			bool fPos = true;
			switch (text[pos])
			{
				case '+':
					++pos;
					break;
				case '-':
					fPos = false;
					++pos;
					break;
			}

			Debug.Assert(text[pos] >= '0' && text[pos] <= '9');
			long intV = 0;
			double dblV = double.NaN;
			bool fFloat = ParseNumberValueIntegerOrDecimal(text, ref pos, ref intV, ref dblV);
			if (fFloat)
				return new JsonNode(fPos ? dblV : -dblV);
			else
				return new JsonNode(fPos ? intV : -intV);
#else
			bool fp = false;
			int epos = pos;
			while (++epos < text.Length)
			{
				char ch = text[epos];
				if (ch == 'e' || ch == 'E')
				{
					fp = true;
					// 吞掉后面的 +/- 号
					var chNext = epos + 1 < text.Length ? text[epos + 1] : (char)0;
					if (chNext == '+' || chNext == '-')
						++epos;
				}
				else if (ch == '.')
					fp = true;
				else if (ch < '0' || ch > '9')
					break;
			}
			string seg = text.Substring(pos, epos - pos);
			pos = epos;
			if (fp)
				return new JsonNode(double.Parse(seg));
			else
				return new JsonNode(long.Parse(seg));
#endif
		}

		static JsonNode ParseInnerTextBooleanValue(string text, ref int pos)
		{
			bool f = ParseBooleanValueToken(text, ref pos);
			return new JsonNode(f);
		}

		static JsonNode ParseInnerTextNoneValue(string text, ref int pos)
		{
			ParseNoneValueToken(text, ref pos);
			return new JsonNode(JsonNodeType.Null);
		}

		static JsonNode ParseInnerTextDictionary(string text, ref int pos)
		{
			Debug.Assert(text[pos] == '{');
			JsonNode node = new JsonNode(JsonNodeType.Dictionary);
			++pos;
			int flag = 0;	// last symbol: 0 init; 1 comma, 2 content

			while (true)
			{
				pos = SeekNextNotSpace(text, pos);
				if (pos < 0)
					throw new LexicalException(pos, "Expect end of dictionary node");

				char ch = text[pos];
				if (ch == '\"')
				{
					// Scan key
					if (flag == 2)
						throw new LexicalException(pos, "Expect , symbol");

					string key = ParseStringValueToken(text, ref pos);
					if (key.Length == 0)
						throw new LexicalException(pos, "Empty key");

					pos = SeekNextNotSpace(text, pos);
					if (pos < 0 || text[pos] != ':')
						throw new LexicalException(pos, "Expect :");
					++pos;

					// Scan value
					JsonNode subnode = ParseInnerTextVariant(text, ref pos);
					subnode.Name = key;
					node.AddDictionaryItem(subnode);

					flag = 2;
				}
				else if (ch == ',')
				{
					if (flag != 2)
						throw new LexicalException(pos, "Unexpected , symbol");
					++pos;
					flag = 1;
				}
				else if (ch == '}')
				{
					if (flag == 1)
						throw new LexicalException(pos, "Unexpected } symbol");
					++pos;
					break;
				}
				else
					throw new LexicalException(pos, "Unexpected symbol");
			}
			return node;
		}

		static JsonNode ParseInnerTextList(string text, ref int pos)
		{
			Debug.Assert(text[pos] == '[');
			JsonNode node = new JsonNode(JsonNodeType.NodeList);
			++pos;
			int flag = 0;	// last symbol: 0 init; 1 comma, 2 content

			while (true)
			{
				pos = SeekNextNotSpace(text, pos);
				if (pos < 0)
					throw new LexicalException(pos, "Expect end of list node");

				char ch = text[pos];
				if (ch == ']')
				{
					if (flag == 1)
						throw new LexicalException(pos, "Unexpected ] symbol");
					++pos;
					break;
				}
				else if (ch == ',')
				{
					if (flag != 2)
						throw new LexicalException(pos, "Unexpected , symbol");
					++pos;
					flag = 1;
				}
				else
				{
					if (flag == 2)
						throw new LexicalException(pos, "Expect , symbol");

					JsonNode subnode = ParseInnerTextVariant(text, ref pos);
					node.AddListItem(subnode);
					
					flag = 2;
				}
			}
			return node;
		}

		public static string ParseStringValueToken(string text, ref int pos)
		{
			Debug.Assert(text[pos] == '\"');
			StringBuilder sb = new StringBuilder();
			++pos;
			while (pos < text.Length)
			{
				char ch = text[pos++];
				if (ch == '\\')
				{
					ch = text[pos++];
					switch (ch)
					{
						case '0':
							sb.Append('\0');	// 0
							break;
						case 'a':
							sb.Append('\a');	// 7 BELL
							break;
						case 'b':
							sb.Append('b');		// 8 Backspace
							break;
						case 't':
							sb.Append('\t');	// 9 Table
							break;
						case 'n':
							sb.Append('\n');	// 10 LF
							break;
						case 'v':
							sb.Append('\v');	// 11 FF
							break;
						case 'f':
							sb.Append('\f');	// 12 FF
							break;
						case 'r':
							sb.Append('\r');	// 13 CR
							break;
						case 'u':
							sb.Append((char)Convert.ToUInt16(text.Substring(pos, 4), 16));
							pos += 4;
							break;
						case '\\':
						case '\'':
						case '\"':
						default:
							sb.Append(ch);
							break;
					}
				}
				else if (ch == '\"')
					return sb.ToString();
				else
					sb.Append(ch);
			}
			throw new LexicalException(pos, "Expect end of value");
		}

		/*
		static bool ParseNumberValueToken(string text, ref int pos, ref long ivalue, ref double fvalue)
		{
			ivalue = ParseNumberValueIntPart(text, ref pos);
			if (pos < text.Length && text[pos] == '.')
			{
				++pos;
				fvalue = ParseNumberValueDecimalPart(text, ref pos) + ivalue;
				return true;
			}
			return false;
		}

        static bool ParseNumberValueIntegerOrDecimal(string text, ref int pos, ref long ivalue, ref double fvalue)
        {
            Debug.Assert(text[pos] >= '0' && text[pos] <= '9');
            string value = "";
            bool isDecimal = false;
            while (pos < text.Length)
            {
                char ch = text[pos];
                if (ch >= '0' && ch <= '9' || ch == '.')
                {
                    value += ch;
                    if (ch == '.')
                        isDecimal = true;
                }
                else
                    break;

                ++pos;
            }

            if (isDecimal)
                fvalue = Convert.ToDouble(value);
            else
                ivalue = Convert.ToInt64(value);
            return isDecimal;  
        }

		static long ParseNumberValueIntPart(string text, ref int pos)
		{
			Debug.Assert(text[pos] >= '0' && text[pos] <= '9');
			long value = 0;
			while (pos < text.Length)
			{
				char ch = text[pos];
				if (ch >= '0' && ch <= '9')
					value = value * 10 + (ch - '0');
				else
					break;

				++pos;
			}
			return value;
		}

		static double ParseNumberValueDecimalPart(string text, ref int pos)
		{
			double value = 0.0, prec = 0.1;
			while (pos < text.Length)
			{
				char ch = text[pos];
				if (ch >= '0' && ch <= '9')
					value += (ch - '0') * prec;
				else
					break;

				++pos;
				prec *= 0.1;
			}
			return value;
		}
		*/

		static bool ParseBooleanValueToken(string text, ref int pos)
		{
			int endpos = pos;
			while (endpos < text.Length)
			{
				char ch = text[endpos];
				if (!((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')))
					break;
				++endpos;
			}

			string token = text.Substring(pos, endpos - pos).ToLowerInvariant();
			pos = endpos;
			if (token == "true")
				return true;
			else if (token == "false")
				return false;
			else 
				throw new LexicalException(pos, "Unexpected value token");
		}

		static void ParseNoneValueToken(string text, ref int pos)
		{
			int endpos = pos;
			while (endpos < text.Length)
			{
				char ch = text[endpos];
				if (!((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')))
					break;
				++endpos;
			}

			string token = text.Substring(pos, endpos - pos).ToLowerInvariant();
			pos = endpos;
			if (token != "null")
				throw new LexicalException(pos, "Unexpected null token");
		}

		static int SeekNextNotSpace(string text, int pos)
		{
			while (pos < text.Length)
			{
				if (text[pos] > 32)
					return pos;
				else
					++pos;
			}
			return -1;
		}
	}
}
