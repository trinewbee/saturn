using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Nano.Json;

namespace Nano.Ext.Marshal
{
    /// <summary>使用 JSON 封送普通对象集合</summary>
    /// <remarks>
    /// 使用 JsonMarshal 封送的对象应该只包含简单的成员变量，或者子对象及子对象集合。
    /// 支持的集合类：List, Dictionary。
    /// 所有子对象应该都是管理关系。如果一个对象被两个父对象引用，它会被封送两次。从而，在解码时创建出两个对象。
    /// </remarks>
    [Obsolete("Use DObject instead of this")]
    public class JsonMarshal
	{
		#region Dumps

		public static JsonNode Dump(object o) => WriteNode(o);

		public static string Dumps(object o)
		{
			var jnode = WriteNode(o);
			JsonWriter jw = new JsonWriter(jnode);
			return jw.GetString();
		}

		static JsonNode WriteNode(object o)
		{
			if (o == null)
				return new JsonNode(JsonNodeType.Null);

			Type rt = o.GetType();
			if (rt.IsGenericType)
			{
				Type gt = rt.GetGenericTypeDefinition();
				if (gt == typeof(List<>))
					return WriteListNode(o);
				else if (gt == typeof(Dictionary<,>))
					return WriteDictionaryNode(o);
				else
					throw new NotSupportedException("Unsupport type name: " + rt.Name);
			}
			else if (rt.IsValueType)
				return WriteValueNode(o, rt);
			else if (rt == typeof(string))
				return new JsonNode((string)o);
			else
				return WriteSimpleObjectNode(rt, o);
		}

		static JsonNode WriteValueNode(object o, Type rt)
		{
			switch (rt.FullName)
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
					return new JsonNode((long)(ulong)o);
				case "System.Boolean":
					return new JsonNode((bool)o);
				case "System.Single":
					return new JsonNode((float)o);
				case "System.Double":
					return new JsonNode((double)o);
				default:
					throw new NotSupportedException("Unsupport type name: " + rt.Name);
			}
		}

		static JsonNode WriteListNode(object o)
		{
			var jnode = new JsonNode(JsonNodeType.NodeList);
			var ls = (System.Collections.IList)o;
			foreach (var ov in ls)
			{
				var jnv = WriteNode(ov);
				jnode.AddChildItem(jnv);
			}
			return jnode;
		}

		static JsonNode WriteDictionaryNode(object o)
		{
			Type rt = o.GetType();
			Type vt = rt.GetGenericArguments()[0];
			if (vt != typeof(string))
				throw new Exception("Key of dictionary must be string");

			var jnode = new JsonNode(JsonNodeType.Dictionary);
			var dc = (System.Collections.IDictionary)o;
			foreach (System.Collections.DictionaryEntry e in dc)
			{
				var jnv = WriteNode(e.Value);
				jnv.Name = (string)e.Key;
				jnode.AddChildItem(jnv);
			}
			return jnode;
		}

		static JsonNode WriteSimpleObjectNode(Type rt, object o)
		{
			var jnode = new JsonNode(JsonNodeType.Dictionary);
			var fieldInfos = rt.GetFields();
			foreach (var fieldInfo in fieldInfos)
			{
				object ov = fieldInfo.GetValue(o);
				var jnv = WriteNode(ov);
				jnv.Name = fieldInfo.Name;
				jnode.AddChildItem(jnv);
			}
			return jnode;
		}

		#endregion

		#region Loads

		public static T Load<T>(JsonNode jnode)
		{
			object o = ReadNode(jnode, typeof(T));
			return (T)o;
		}

		public static T Loads<T>(string jstr)
		{
			var jnode = JsonParser.ParseText(jstr);
			return Load<T>(jnode);
		}

		static object ReadNode(JsonNode jnode, Type rt)
		{
			if (jnode.NodeType == JsonNodeType.Null)
				return null;

			if (rt.IsGenericType)
			{
				Type gt = rt.GetGenericTypeDefinition();
				if (gt == typeof(List<>))
					return ReadListNode(jnode, rt);
				else if (gt == typeof(Dictionary<,>))
					return ReadDictionaryNode(jnode, rt);
				else
					throw new NotSupportedException("Unsupport type name: " + rt.Name);
			}
			else if (rt.IsValueType)
				return ReadValueNode(jnode, rt);
			else if (rt == typeof(string))
				return jnode.TextValue;
			else
				return ReadSimpleObjectNode(jnode, rt);
		}

		static object ReadValueNode(JsonNode jnode, Type rt)
		{
			switch (rt.FullName)
			{
				case "System.Byte":
					return (byte)jnode.IntValue;
				case "System.SByte":
					return (sbyte)jnode.IntValue;
				case "System.Int16":
					return (short)jnode.IntValue;
				case "System.UInt16":
					return (ushort)jnode.IntValue;
				case "System.Int32":
					return (int)jnode.IntValue;
				case "System.UInt32":
					return (uint)jnode.IntValue;
				case "System.Int64":
					return jnode.IntValue;
				case "System.UInt64":
					return (ulong)jnode.IntValue;
				case "System.Boolean":
					return jnode.BoolValue;
				case "System.Single":
					return jnode.NodeType == JsonNodeType.Float ? (float)jnode.FloatValue : (float)jnode.IntValue;
				case "System.Double":
					return jnode.NodeType == JsonNodeType.Float ? jnode.FloatValue : (double)jnode.IntValue;
				default:
					throw new NotSupportedException("Unsupport type name: " + rt.Name);
			}
		}

		static object ReadListNode(JsonNode jnode, Type rt)
		{
			Debug.Assert(jnode.NodeType == JsonNodeType.NodeList);
			Type vt = rt.GetGenericArguments()[0];
			var ls = (System.Collections.IList)Activator.CreateInstance(rt);
			foreach (var jnv in jnode.ChildNodes)
			{
				object ov = ReadNode(jnv, vt);
				ls.Add(ov);
			}
			return ls;
		}

		static object ReadDictionaryNode(JsonNode jnode, Type rt)
		{
			Debug.Assert(jnode.NodeType == JsonNodeType.Dictionary);
			Type[] vts = rt.GetGenericArguments();
			if (vts[0] != typeof(string))
				throw new Exception("Key of dictionary must be string");

			var dc = (System.Collections.IDictionary)Activator.CreateInstance(rt);
			foreach (var jnv in jnode.ChildNodes)
			{
				object ov = ReadNode(jnv, vts[1]);
				dc.Add(jnv.Name, ov);
			}
			return dc;
		}

		static object ReadSimpleObjectNode(JsonNode jnode, Type rt)
		{
			Debug.Assert(jnode.NodeType == JsonNodeType.Dictionary);
			object o = Activator.CreateInstance(rt);
			var fieldInfos = rt.GetFields();
			foreach (var fieldInfo in fieldInfos)
			{
				var jnv = jnode[fieldInfo.Name];
				object ov = ReadNode(jnv, fieldInfo.FieldType);
				fieldInfo.SetValue(o, ov);
			}
			return o;
		}

		#endregion
	}
}
