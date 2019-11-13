using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Nano.Json;

namespace Nano.Ext.Marshal
{
	/// <summary>扩展的 JSON 辅助类</summary>
    [Obsolete("Use DObject instead of this")]
	public class JsonModel
	{
		/// <summary>将给定对象转换成 JSON 文本</summary>
		/// <param name="o">待转换对象</param>
		/// <returns>JSON 文本</returns>
		public static string Dumps(object o)
		{
			var node = Dump(o);
			var wr = new JsonWriter();
			wr.WriteNode(null, node);
			wr.Complete();
			return wr.GetString();
		}

		/// <summary>将给定对象转换成 JSON 对象</summary>
		/// <param name="o">待转换对象</param>
		/// <returns>JSON 对象</returns>
		public static JsonNode Dump(object o)
		{
			if (o == null)
				return new JsonNode(JsonNodeType.Null);

			var vt = o.GetType();
			string tname = vt.FullName;

			if (vt.IsPrimitive || tname == "System.String")
				return DumpValue(tname, o);

			if (vt.IsArray)
				return DumpArray(o);

            if (vt == typeof(JsonNode))
                return (JsonNode)o;

            if (tname.StartsWith("System.Collections.Generic.Dictionary`2"))
			{
				var dict = (System.Collections.IDictionary)o;
				var node = new JsonNode(JsonNodeType.Dictionary);
				foreach (System.Collections.DictionaryEntry pair in dict)
				{
					var subnode = Dump(pair.Value);
					subnode.Name = (string)pair.Key;
					node.AddChildItem(subnode);
				}
				return node;
			}
			else if (tname.StartsWith("System.Collections.Generic.List`1"))
			{
				var list = (System.Collections.IList)o;
				var node = new JsonNode(JsonNodeType.NodeList);
				foreach (var val in list)
				{
					var subnode = Dump(val);
					node.AddChildItem(subnode);
				}
				return node;
			}
			else
				return DumpObject(vt, o);
		}

		static JsonNode DumpValue(string tname, object o)
		{
			switch (tname)
			{
				case "System.Byte":
					return new JsonNode((byte)o);
				case "System.SByte":
					return new JsonNode((sbyte)o);
				case "System.Int16":
					return new JsonNode((short)o);
				case "System.UInt16":
					return new JsonNode((ushort)o);
				case "System.Int32":
					return new JsonNode((int)o);
				case "System.UInt32":
					return new JsonNode((uint)o);
				case "System.Int64":
					return new JsonNode((long)o);
				case "System.UInt64":
					return new JsonNode((ulong)o);
				case "System.Boolean":
					return new JsonNode((bool)o);
				case "System.String":
					return new JsonNode((string)o);
				case "System.Single":
					return new JsonNode((float)o);
				case "System.Double":
					return new JsonNode((double)o);
				default:
					throw new NotSupportedException("Invalid value type: " + tname);
			}
		}

		static JsonNode DumpArray(object o)
		{
			var arr = (Array)o;
			var jn = new JsonNode(JsonNodeType.NodeList);
			foreach (var oi in arr)
			{
				var jni = Dump(oi);
				jn.AddChildItem(jni);
			}
			return jn;
		}

		static JsonNode DumpObject(Type vt, object o)
		{
			var jnode = new JsonNode(JsonNodeType.Dictionary);

			var fields = vt.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var field in fields)
			{
				var oi = field.GetValue(o);
				var jni = Dump(oi);
				jni.Name = field.Name;
				jnode.AddChildItem(jni);
			}

			var props = vt.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var prop in props)
			{
				var prms = prop.GetIndexParameters();
				if (prms != null && prms.Length != 0)
					continue;

				var oi = prop.GetValue(o, null);
				var jni = Dump(oi);
				jni.Name = prop.Name;
				jnode.AddChildItem(jni);
			}

			return jnode;
		}

		/// <summary>将给定 JSON 文本转换成对象</summary>
		/// <param name="str">给定的 JSON 文本</param>
		/// <returns>转换后的对象</returns>
		/// <remarks>参见 Load 方法</remarks>
		public static object Loads(string str)
		{
			var node = JsonParser.ParseText(str);
			return Load(node);
		}

		/// <summary>将给定 JSON 对象转换成对象</summary>
		/// <param name="node">给定的 JSON 对象</param>
		/// <returns>转换后的对象</returns>
		/// <remarks>
		/// 数值对象会分别转换成 long, double, bool, string 值，
		/// 字典会转换成 Dictionary&lt;string, object&gt; 对象，
		/// 列表会转换成 List&lt;object&gt; 对象，
		/// Null 节点会转换成 null。
		/// </remarks>
		public static object Load(JsonNode node)
		{
			switch (node.NodeType)
			{
				case JsonNodeType.Integer:
				case JsonNodeType.Float:
				case JsonNodeType.Boolean:
				case JsonNodeType.String:
					return node.Value;
				case JsonNodeType.Null:
					return null;
				case JsonNodeType.NodeList:
					return LoadJsonList(node);
				case JsonNodeType.Dictionary:
					return LoadJsonDict(node);
				default:
					throw new ArgumentException("Invalid JsonNodeType: " + node.NodeType);
			}
		}

		static List<object> LoadJsonList(JsonNode node)
		{
			Debug.Assert(node.NodeType == JsonNodeType.NodeList);
			var ls = new List<object>();
			foreach (var nodeSub in node.ChildNodes)
			{
				object o = Load(nodeSub);
				ls.Add(o);
			}
			return ls;
		}

		static Dictionary<string, object> LoadJsonDict(JsonNode node)
		{
			Debug.Assert(node.NodeType == JsonNodeType.Dictionary);
			var dc = new Dictionary<string, object>();
			foreach (var nodeSub in node.ChildNodes)
			{
				object o = Load(nodeSub);
				dc.Add(nodeSub.Name, o);
			}
			return dc;
		}

		static bool Equals(double x, double y)
		{
			if (x == 0.0)
				return y == 0.0;
			return Math.Abs(x - y) / Math.Abs(x + y) < 1e-14;
		}

		/// <summary>判断两个 JSON 对象是否相等</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果相等返回 true，否则返回 false</returns>
		/// <remarks>
		/// 值对象会比较类型和值，其中浮点数会按照误差在 1e-14 范围内比较。
		/// 字典对象会先比较子对象数目，然后按照 key 逐一比较每一个子对象，
		/// 列表对象会先比较子对象数目，然后按照索引逐一比较每一个子对象。
		/// Null 对象只和 Null 对象相等。
		/// </remarks>
		public static bool Equals(JsonNode x, JsonNode y)
		{
			if (x.NodeType != y.NodeType)
				return false;

			switch (x.NodeType)
			{
				case JsonNodeType.Integer:
					return x.IntValue == y.IntValue;
				case JsonNodeType.Float:
					return Equals(x.FloatValue, y.FloatValue);
				case JsonNodeType.Boolean:
					return x.BoolValue == y.BoolValue;
				case JsonNodeType.String:
					return x.TextValue == y.TextValue;
				case JsonNodeType.Null:
					return true;
				case JsonNodeType.NodeList:
					return JsonListEquals(x, y);
				case JsonNodeType.Dictionary:
					return JsonDictEquals(x, y);
				default:
					throw new ArgumentException("Invalid JsonNodeType: " + x.NodeType);
			}
		}

		static bool JsonListEquals(JsonNode x, JsonNode y)
		{
			Debug.Assert(x.NodeType == JsonNodeType.NodeList && y.NodeType == JsonNodeType.NodeList);
			if (x.ChildCount != y.ChildCount)
				return false;
			for (int i = 0; i < x.ChildCount; ++i)
			{
				if (!Equals(x[i], y[i]))
					return false;
			}
			return true;
		}

		static bool JsonDictEquals(JsonNode x, JsonNode y)
		{
			Debug.Assert(x.NodeType == JsonNodeType.Dictionary && y.NodeType == JsonNodeType.Dictionary);
			if (x.ChildCount != y.ChildCount)
				return false;
			foreach (var node in x.ChildNodes)
			{
				var node2 = y[node.Name];
				if (node2 == null || !Equals(node, node2))
					return false;
			}
			return true;
		}
	}
}
