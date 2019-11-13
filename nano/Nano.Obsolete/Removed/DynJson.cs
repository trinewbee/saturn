using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using Nano.Collection;
using Nano.Json;

namespace Nano.Ext.Marshal
{
	// DynJsonNode is no longer supported, use Nano.Nuts.DObject instead

	/*
	class DynJsonNode : DynamicObject
	{
		JsonNode m_node;

		public DynJsonNode(JsonNode node) { m_node = node; }

		#region Tools

		static bool GetValue(JsonNode jn, Type vt, out object result)
		{
			switch (vt.FullName)
			{
				case "System.Int32":
					result = (int)jn.IntValue;
					return true;
				case "System.Int64":
					result = jn.IntValue;
					return true;
				case "System.Single":
					result = (float)GetFloat(jn);
					return true;
				case "System.Double":
					result = GetFloat(jn);
					return true;
				case "System.String":
					result = jn.TextValue;
					return true;
				case "System.Boolean":
					result = jn.BoolValue;
					return true;
				default:
					result = null;
					return false;
			}
		}

		static double GetFloat(JsonNode jn)
		{
			switch (jn.NodeType)
			{
				case JsonNodeType.Float:
					return jn.FloatValue;
				case JsonNodeType.Integer:
					return (double)jn.IntValue;
				default:
					throw new InvalidCastException();
			}
		}

		#endregion

		#region Exports

		public static implicit operator int(DynJsonNode jn) => (int)jn.m_node.IntValue;

		public static implicit operator long(DynJsonNode jn) => jn.m_node.IntValue;

		public static implicit operator float(DynJsonNode jn) => (float)GetFloat(jn.m_node);

		public static implicit operator double(DynJsonNode jn) => GetFloat(jn.m_node);

		public static implicit operator string(DynJsonNode jn) => jn.m_node.TextValue;

		public static implicit operator bool(DynJsonNode jn) => jn.m_node.BoolValue;

		public static bool operator true(DynJsonNode jn) => jn.m_node.BoolValue;

		public static bool operator false(DynJsonNode jn) => !jn.m_node.BoolValue;

		public bool IsNull() => m_node.IsNull;

		public JsonNode GetNode() => m_node;

		public IEnumerable<dynamic> List()
		{
			foreach (var node in m_node.ChildNodes)
				yield return new DynJsonNode(node);
		}

		#endregion

		#region Dynamic

		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			if (GetValue(m_node, binder.Type, out result))
				return true;
			return base.TryConvert(binder, out result);
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (m_node.NodeType == JsonNodeType.Dictionary)
			{
				var name = binder.Name;
				var node = m_node[name];
				result = new DynJsonNode(node);
				return true;
			}
			return base.TryGetMember(binder, out result);
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (indexes.Length == 1)
			{
				object key = indexes[0];
				if (m_node.NodeType == JsonNodeType.Dictionary)
				{
					if (key is string)
					{
						result = new DynJsonNode(m_node[(string)key]);
						return true;
					}
				}
				else if (m_node.NodeType == JsonNodeType.NodeList)
				{
					if (key is int)
					{
						result = new DynJsonNode(m_node[(int)key]);
						return true;
					}
				}
			}
			return base.TryGetIndex(binder, indexes, out result);
		}

		public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
		{
			switch (m_node.NodeType)
			{
				case JsonNodeType.Boolean:
					return TryUopBool(binder.Operation, out result);
				default:
					result = null;
					return false;
			}
		}

		bool TryUopBool(ExpressionType op, out object result)
		{
			bool lv = m_node.BoolValue;
			switch (op)
			{
				case ExpressionType.Not:
					result = !lv;
					return true;
				case ExpressionType.IsTrue:
					result = lv;
					return true;
				default:
					result = null;
					return false;
			}
		}

		public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
		{
			switch (m_node.NodeType)
			{
				case JsonNodeType.Integer:
					return TryBopInt(binder.Operation, arg, out result);
				case JsonNodeType.Float:
					return TryBopDbl(binder.Operation, arg, out result);
				case JsonNodeType.String:
					return TryBopStr(binder.Operation, arg, out result);
				default:
					result = null;
					return false;
			}
		}

		bool TryBopInt(ExpressionType op, object arg, out object result)
		{
			long lv = m_node.IntValue;
			long rv = Convert.ToInt64(arg);
			switch (op)
			{
				case ExpressionType.Equal:
					result = lv == rv;
					return true;
				default:
					result = null;
					return false;
			}
		}

		bool TryBopDbl(ExpressionType op, object arg, out object result)
		{
			double lv = GetFloat(m_node);
			double rv = Convert.ToDouble(arg);
			switch (op)
			{
				case ExpressionType.Equal:
					result = lv == rv;
					return true;
				default:
					result = null;
					return false;
			}
		}

		bool TryBopStr(ExpressionType op, object arg, out object result)
		{
			string lv = m_node.TextValue;
			string rv = arg.ToString();
			switch (op)
			{
				case ExpressionType.Equal:
					result = lv == rv;
					return true;
				default:
					result = null;
					return false;
			}
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			var name = binder.Name;
			switch (name)
			{
				case "List":
					result = List();
					return true;
				case "IsNull":
					result = IsNull();
					return true;
				case "GetNode":
					result = GetNode();
					return true;
			}

			return base.TryInvokeMember(binder, args, out result);
		}

		#endregion
	}

	/// <summary>动态 JSON 类</summary>
	public static class DynJson
	{
		/// <summary>将对象转换成动态 JSON 对象</summary>
		/// <param name="o">传入对象</param>
		/// <returns>经转换的动态 JSON 对象</returns>
		public static dynamic Dump(object o)
		{
			var jnode = JsonModel.Dump(o);
			return new DynJsonNode(jnode);
		}

		/// <summary>将对象转换成 JSON 字符串</summary>
		/// <param name="o">传入对象</param>
		/// <returns>经转换的 JSON 字符串</returns>
		public static string Dumps(object o)
		{
			if (o is DynJson)
				return ((DynJsonNode)o).GetNode();
			return JsonModel.Dumps(o);
		}

		/// <summary>将 JSON 字符串转换成动态 JSON 对象</summary>
		/// <param name="s">JSON 字符串</param>
		/// <returns>经转换的动态 JSON 对象</returns>
		public static dynamic Loads(string s)
		{
			var jnode = JsonParser.ParseText(s);
			return new DynJsonNode(jnode);
		}
	}
	*/
}
