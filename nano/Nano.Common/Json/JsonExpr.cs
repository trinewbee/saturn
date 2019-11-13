using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Common;

namespace Nano.Json.Expression
{
	/// <summary>Json 构造表达式</summary>
	public class JE
	{
		/// <summary>Json 构造表达式普通节点</summary>
		public struct Expr
		{
			public string Key;
			public JsonNode Node;

			public Expr(string _key, JsonNode _node)
			{
				Key = _key;
				Node = _node;
			}
		}

		/// <summary>Json 构造表达式组合起始节点</summary>
		public struct BExpr
		{
			public string Key;
			public JsonNodeType NodeType;

			public BExpr(string _key, JsonNodeType _type)
			{
				Key = _key;
				NodeType = _type;
			}
		}

		/// <summary>Json 构造表达式组合结束节点</summary>
		public struct EExpr
		{
			public JsonNodeType NodeType;

			public EExpr(JsonNodeType type)
			{
				NodeType = type;
			}
		}

		private JsonWriter m_wr;

		/// <summary>默认构造</summary>
		/// <remarks>请使用 JE.New() 函数</remarks>
		public JE()
		{
			m_wr = new JsonWriter();
		}

		/// <summary>基于给定 JsonNode 构造</summary>
		/// <param name="node">JsonNode</param>
		/// <param name="fReopen">是否可以增加元素</param>
		public JE(JsonNode node, bool fReopen)  // Re-open function
		{
			m_wr = new JsonWriter(node, fReopen);
		}

		public static JE New() => new JE();

		public static JE Set(JsonNode node) => new JE(node, false);

		public static JE Reopen(JsonNode node) => new JE(node, true);

		public static Expr Pair(string key, JsonNode node) => new Expr(key, node);

		public static Expr Pair(string key, string value) => new Expr(key, new JsonNode(value));

		public static Expr Pair(string key, long value) => new Expr(key, new JsonNode(value));

		public static Expr Pair(string key, bool value) => new Expr(key, new JsonNode(value));

		public static Expr Pair(string key, JE sub)
		{
			JsonNode node = sub.Complete();
			return new Expr(key, node);
		}

		public static Expr Null(string key) => Pair(key, new JsonNode(JsonNodeType.Null));

		public static Expr Null() => Null(null);

		public static BExpr Dict(string key) => new BExpr(key, JsonNodeType.Dictionary);

		public static BExpr Dict() => Dict(null);

		public static BExpr List(string key) => new BExpr(key, JsonNodeType.NodeList);

		public static BExpr List() => List(null);

		public static EExpr EDict() => new EExpr(JsonNodeType.Dictionary);

		public static EExpr EList() => new EExpr(JsonNodeType.NodeList);

		public static JE operator +(JE e, Expr rhs)
		{
			e.m_wr.WriteNode(rhs.Key, rhs.Node);
			return e;
		}

		public static JE operator +(JE e, BExpr ths)
		{
			switch (ths.NodeType)
			{
				case JsonNodeType.Dictionary:
					e.m_wr.BeginDictionary(ths.Key);
					break;
				case JsonNodeType.NodeList:
					e.m_wr.BeginList(ths.Key);
					break;
				default:
					throw new ArgumentException("Unsupported node type");
			}
			return e;
		}

		public static JE operator +(JE e, EExpr rhs)
		{
			switch (rhs.NodeType)
			{
				case JsonNodeType.Dictionary:
					e.m_wr.EndDictionary();
					break;
				case JsonNodeType.NodeList:
					e.m_wr.EndList();
					break;
				default:
					throw new ArgumentException("Unsupported node type");
			}
			return e;
		}

		public static JE operator +(JE e, string value)
		{
			e.m_wr.WriteString(null, value);
			return e;
		}

		public static JE operator +(JE e, long value)
		{
			e.m_wr.WriteInt(null, value);
			return e;
		}

		public static JE operator +(JE e, double value)
		{
			e.m_wr.WriteFloat(null, value);
			return e;
		}

		public static JE operator +(JE e, bool value)
		{
			e.m_wr.WriteBool(null, value);
			return e;
		}

		public static JE operator +(JE e, JsonNode node)
		{
			e.m_wr.WriteNode(null, node);
			return e;
		}

		public static JE operator +(JE e, JE sub)
		{
			JsonNode node = sub.Complete();
			e.m_wr.WriteNode(null, node);
			return e;
		}

		public string GetString()
		{
			JsonNode node = Complete();
			JsonExpressFormater jsf = new JsonExpressFormater();
			return jsf.Format(node);
		}

		public string GetString(JsonExpressFormater jsf)
		{
			JsonNode node = Complete();
			return jsf.Format(node);
		}

		public JsonNode Complete() => m_wr.Complete();
	}
}
