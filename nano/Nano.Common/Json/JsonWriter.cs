using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Nano.Lexical;

namespace Nano.Json
{
	public class JsonExpressFormater
	{
		public string IndentChars = null;
		public string NewLineChars = null;

        static string[] ECHS = new string[32] {
            "\\0", "\\u0001", "\\u0002", "\\u0003", "\\u0004", "\\u0005", "\\u0006", "\\u0007",
            "\\b", "\\t", "\\n", "\\u000b", "\\f", "\\r", "\\u000e", "\\u000f",
            "\\u0010", "\\u0011", "\\u0012", "\\u0013", "\\u0014", "\\u0015", "\\u0016", "\\u0017",
            "\\u0018", "\\u0019", "\\u001a", "\\u001b", "\\u001c", "\\u001d", "\\u001e", "\\u001f"
        };

		public string Format(JsonNode node)
		{
			StringBuilder sb = new StringBuilder();
			WriteExpression(null, node, sb, 0);

			return sb.ToString();
		}

        public static string EscapeString(string str)
        {
            var sb = new StringBuilder();
            sb.Append('\"');

            if (str != null)
            {
                foreach (char ch in str)
                {
                    if (ch < ' ')
                    {
                        sb.Append(ECHS[ch]);
                    }
                    else
                    {
                        switch (ch)
                        {
                            case '\\':
                                sb.Append("\\\\");
                                break;
                            case '\"':
                                sb.Append("\\\"");
                                break;
                            default:
                                sb.Append(ch);
                                break;
                        }
                    }
                }
            }
            sb.Append('\"');
            return sb.ToString();
        }

		void WriteExpression(string name, JsonNode node, StringBuilder sb, int level)
		{
			switch (node.NodeType)
			{
				case JsonNodeType.String:
					WriteValue(name, EscapeString(node.TextValue), sb, level);
					break;
				case JsonNodeType.Integer:
					WriteValue(name, Convert.ToString(node.IntValue), sb, level);
					break;
				case JsonNodeType.Float:
					WriteValue(name, Convert.ToString(node.FloatValue), sb, level);
					break;
				case JsonNodeType.Boolean:
					WriteValue(name, node.BoolValue ? "true" : "false", sb, level);
					break;
				case JsonNodeType.Null:
					WriteValue(name, "null", sb, level);
					break;
				case JsonNodeType.Dictionary:
					WriteSubDictionary(name, node, sb, level);
					break;
				case JsonNodeType.NodeList:
					WriteSubList(name, node, sb, level);
					break;
				default:
					throw new LexicalException(0, "Unsupported node type");
			}
		}

		void WriteValue(string name, string value, StringBuilder sb, int level)
		{
			// Write indents
			if (IndentChars != null)
				for (int i = 0; i < level; ++i)
					sb.Append(IndentChars);

			// Write name
			if (name != null)
			{
				sb.Append('\"');
				sb.Append(name);
				sb.Append("\":");
			}

			sb.Append(value);
		}

		void WriteNewLine(StringBuilder sb)
		{
			if (NewLineChars != null)
				sb.Append(NewLineChars);
		}

		void WriteSubDictionary(string name, JsonNode node, StringBuilder sb, int level)
		{
			WriteValue(name, "{", sb, level);
			WriteNewLine(sb);

			int count = node.ChildCount;
			foreach (JsonNode subnode in node.ChildNodes)
			{
				Debug.Assert(subnode.Name != null);
				WriteExpression(subnode.Name, subnode, sb, level + 1);
				if (--count > 0)
					sb.Append(',');
				WriteNewLine(sb);
			}
			Debug.Assert(count == 0);

			WriteValue(null, "}", sb, level);
		}

		void WriteSubList(string name, JsonNode node, StringBuilder sb, int level)
		{
			WriteValue(name, "[", sb, level);
			WriteNewLine(sb);

			int count = node.ChildCount;
			foreach (JsonNode subnode in node.ChildNodes)
			{
				Debug.Assert(subnode.Name == null);
				WriteExpression(null, subnode, sb, level + 1);
				if (--count > 0)
					sb.Append(',');
				WriteNewLine(sb);
			}

			WriteValue(null, "]", sb, level);
		}
	}

	public class JsonWriter
	{
		public string IndentChars = null;
		public string NewLineChars = null;

		JsonNode m_root, m_top;
		Stack<JsonNode> m_stack;

		public JsonWriter()
		{
			Reset();
		}

		// Re-open root node for appending items, if it is a collection node
		public JsonWriter(JsonNode root, bool fReopen = false)
		{
			Reset();
			m_root = root;
			if (fReopen && (root.NodeType == JsonNodeType.Dictionary || root.NodeType == JsonNodeType.NodeList))
			{
				m_top = m_root;
				m_stack.Push(m_top);
			}
		}
	
		public void Reset()
		{
			m_root = m_top = null;
			m_stack = new Stack<JsonNode>();
		}

		public void WriteNode(string name, JsonNode node)
		{
			if (m_root != null)
			{
				// Add a child node under a list or dictionary node
				if (m_top == null)
					throw new LexicalException(0, "Not under a collection node");

				Debug.Assert(m_stack.Count != 0);
				node.Name = name;
				m_top.AddChildItem(node);
			}
			else
			{
				// The root element
				Debug.Assert(m_stack.Count == 0 && m_top == null);
				if (name != null)
					throw new LexicalException(0, "Node must not have a name");

				m_root = node;
			}
		}

		public void WriteString(string name, string value)
		{
			WriteNode(name, new JsonNode(value));
		}
	
		public void WriteInt(string name, long value)
		{
			WriteNode(name, new JsonNode(value));
		}

		public void WriteFloat(string name, double value)
		{
			WriteNode(name, new JsonNode(value));
		}

		public void WriteBool(string name, bool value)
		{
			WriteNode(name, new JsonNode(value));
		}

		public void WriteNull(string name)
		{
			WriteNode(name, new JsonNode(JsonNodeType.Null));
		}

		public void BeginDictionary(string name)
		{
			JsonNode node = new JsonNode(JsonNodeType.Dictionary);
			WriteNode(name, node);
			m_stack.Push(m_top = node);
		}

		public void EndDictionary()
		{
			if (m_top == null || m_top.NodeType != JsonNodeType.Dictionary)
				throw new LexicalException(0, "No matched dictionary node");

			m_stack.Pop();
			m_top = m_stack.Count() != 0 ? m_stack.Peek() : null;
		}

		public void BeginList(string name)
		{
			JsonNode node = new JsonNode(JsonNodeType.NodeList);
			WriteNode(name, node);
			m_stack.Push(m_top = node);	
		}

		public void EndList()
		{
			if (m_top == null || m_top.NodeType != JsonNodeType.NodeList)
				throw new LexicalException(0, "No matched list node");

			m_stack.Pop();
			m_top = m_stack.Count() != 0 ? m_stack.Peek() : null;
		}

		public JsonNode Complete()
		{
			if (m_top != null)
				throw new LexicalException(0, "Not all nodes closed");
			if (m_root == null)
				throw new LexicalException(0, "Root node not set");

			Debug.Assert(m_stack.Count == 0);
			return m_root;
		}

		public string GetString()
		{
			JsonNode root = Complete();
			JsonExpressFormater jsf = new JsonExpressFormater();
			jsf.IndentChars = IndentChars;
			jsf.NewLineChars = NewLineChars;
			return jsf.Format(root);
		}
	}
}
