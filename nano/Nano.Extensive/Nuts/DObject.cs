using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Nano.Json;

namespace Nano.Nuts
{
    /// <summary>Json 节点类</summary>
    /// <remarks>本对象用于替代 JsonNode 以及衍生类</remarks>
    public class DObject
    {
        public class DList : List<DObject>
        {
            public void Add(object o) => base.Add(New(o));

            public DObject ToObject() => DObject.New(this);

            public JsonNode ToJson() => ToObject().ToJson();

            public static implicit operator DObject(DList value) => value.ToObject();
        }

        public class DMap : Dictionary<string, DObject>
        {
            public void Add(string key, object o) => base.Add(key, New(o));

            public DObject ToObject() => DObject.New(this);

            public JsonNode ToJson() => ToObject().ToJson();

            public static implicit operator DObject(DMap value) => value.ToObject();
        }

        object m_value = null;

        #region Tools

        static double GetFloat(object value)
        {
            if (value is double)
                return (double)value;
            else if (value is long)
                return (long)value;
            else
                throw new InvalidCastException();
        }

        #endregion

        #region Properties

        static Dictionary<Type, JsonNodeType> _node_type_map = new Dictionary<Type, JsonNodeType>
        {
            [typeof(long)] = JsonNodeType.Integer,
            [typeof(double)] = JsonNodeType.Float,
            [typeof(string)] = JsonNodeType.String,
            [typeof(bool)] = JsonNodeType.Boolean,
            [typeof(DList)] = JsonNodeType.NodeList,
            [typeof(DMap)] = JsonNodeType.Dictionary,
        };

        public JsonNodeType NodeType
        {
            get
            {
                if (m_value == null)
                    return JsonNodeType.Null;

                JsonNodeType result;
                var vt = m_value.GetType();
                if (_node_type_map.TryGetValue(vt, out result))
                    return result;

                throw new ArgumentException("Wrong DObject type: " + vt.Name);
            }
        }

        public static DObject Null => new DObject { m_value = null };

        public bool IsNull => m_value == null;

        public bool IsInt => m_value is long;

        public bool IsFloat => m_value is double;

        public bool IsBool => m_value is bool;

        public bool IsString => m_value is string;

        public bool IsList => m_value is DList;

        public bool IsMap => m_value is DMap;

        public DList List => (DList)m_value;

        public DMap Map => (DMap)m_value;

        #endregion

        #region 创建对象

        const System.Reflection.BindingFlags bind_flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        static readonly Type BaseMapType = typeof(System.Collections.IDictionary);
        static readonly Type BaseListType = typeof(System.Collections.IList);

        public static DObject New(object o)
        {
            if (o == null)
                return new DObject();

            var vt = o.GetType();
            if (_node_type_map.ContainsKey(vt))            
                return new DObject { m_value = o };

            if (vt.IsPrimitive)
            {
                if (vt == typeof(int))
                    return new DObject { m_value = (long)(int)o };
                else if (vt == typeof(uint))
                    return new DObject { m_value = (long)(uint)o };
                else if (vt == typeof(ulong))
                    return new DObject { m_value = (long)(ulong)o };
                else if (vt == typeof(float))
                    return new DObject { m_value = (double)(float)o };
                else // byte, sbyte, short, ushort, ...
                    throw new NotSupportedException();
            }
            else if (vt == typeof(DObject))
                return new DObject { m_value = ((DObject)o).m_value };
            else if (vt == typeof(JsonNode))
                return ImportJson((JsonNode)o);
            else if (vt.IsArray)
            {
                var arr = (Array)o;
                if (arr.Rank != 1)
                    throw new NotSupportedException("MuitiDimArrayNotSupported");
                return Transform(arr, oi => New(oi));
            }
            else if (IsCompilerGenerated(vt)) // anonymous class
            {
                var props = vt.GetProperties(bind_flags);
                var map = new DMap();
                foreach (var prop in props)
                {
                    var oi = prop.GetValue(o, null);
                    map.Add(prop.Name, oi);
                }
                return New(map);
            }
            else if (BaseMapType.IsInstanceOfType(o)) // IDictionary
                return _NewMap((System.Collections.IDictionary)o);
            else if (BaseListType.IsInstanceOfType(o)) // IList
                return _NewList((System.Collections.IList)o);
            else
                return _NewNormalClass(o, vt);
        }

        static DObject _NewMap(System.Collections.IDictionary map)
        {
            var dmap = new DMap();
            foreach (System.Collections.DictionaryEntry pair in map)
            {
                var di = New(pair.Value);
                dmap.Add(pair.Key.ToString(), di);
            }
            return New(dmap);
        }

        static DObject _NewList(System.Collections.IList ls)
        {
            var dls = new DList();
            foreach (object o in ls)
            {
                var di = New(o);
                dls.Add(di);
            }
            return New(dls);
        }

        static DObject _NewNormalClass(object o, Type vt)
        {
            var fields = vt.GetFields(bind_flags);
            var map = new DMap();
            foreach (var field in fields)
            {
                var oi = field.GetValue(o);
                map.Add(field.Name, oi);
            }
            return New(map);
        }

        /// <summary>判断给定类型是否为匿名类</summary>
        /// <param name="vt">类型</param>
        /// <returns>返回给定类型是否为匿名类</returns>
        /// <remarks>该函数检查 CompilerGeneratedAttribute 属性。</remarks>
        public static bool IsCompilerGenerated(Type vt)
        {
            var attrs = vt.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
            if (attrs.Length != 0)
            {
                Debug.Assert(vt.Name.StartsWith("<>f__AnonymousType"));
                return true;
            }
            else
                return false;
        }

        public static DObject Transform(System.Collections.IEnumerable e, Func<object, DObject> tr, Predicate<object> where = null)
        {
            var ls = new DList();
            foreach (object x in e)
            {
                if (where != null && !where(x))
                    continue;
                ls.Add(tr(x));
            }
            return new DObject { m_value = ls };
        }

        public static DObject Transform<T>(IEnumerable<T> e, Func<T, DObject> tr, Predicate<T> where = null)
        {
            var ls = new DList();
            foreach (T x in e)
            {
                if (where != null && !where(x))
                    continue;
                ls.Add(tr(x));
            }
            return new DObject { m_value = ls };
        }

        public static DObject TransformMap<T>(IDictionary<string, T> map, Func<T, DObject> tr, Predicate<T> where = null)
        {
            var dmap = new DMap();
            foreach (var pair in map)
            {
                if (where != null && !where(pair.Value))
                    continue;
                dmap.Add(pair.Key, tr(pair.Value));
            }
            return new DObject { m_value = dmap };
        }

        #endregion

        #region 类型转换

        public static implicit operator DObject(long value) => new DObject { m_value = value };

        public static implicit operator DObject(double value) => new DObject { m_value = value };

        public static implicit operator DObject(string value) => new DObject { m_value = value };

        public static implicit operator DObject(bool value) => new DObject { m_value = value };

        public static implicit operator DObject(Array array) => New(array);

		public static implicit operator long(DObject o) => (long)o.m_value;

		public static implicit operator double(DObject o) => GetFloat(o.m_value);

		public static implicit operator string(DObject o) => (string)o.m_value;

		public static implicit operator bool(DObject o) => (bool)o.m_value;

		public static bool operator true(DObject o) => (bool)o.m_value;

		public static bool operator false(DObject o) => !(bool)o.m_value;

        public JsonNode ToJson() => ExportJson(this);

		/// <summary>将对象树转换为文本/summary>
		/// <returns>JSON 格式字符串</returns>
		public override string ToString()
		{
			if (m_value == null)
				return "null";
			var vt = m_value.GetType();
			if (vt.IsPrimitive)
				return vt != typeof(bool) ? m_value.ToString() : ((bool)m_value ? "true" : "false");
			else if (vt == typeof(string))
				return JsonExpressFormater.EscapeString((string)m_value);
			else if (vt == typeof(DList))
			{
				var sb = new StringBuilder().Append('[');
				foreach (var item in (DList)m_value)
				{
					sb.Append(item.ToString());
					sb.Append(',');
				}
				if (sb.Length > 1)
					sb.Remove(sb.Length - 1, 1);
				sb.Append(']');
				return sb.ToString();
			}
			else if (vt == typeof(DMap))
			{
				var sb = new StringBuilder().Append('{');
				foreach (var pair in (DMap)m_value)
				{
					sb.Append(JsonExpressFormater.EscapeString(pair.Key));
					sb.Append(':');
					sb.Append(pair.Value.ToString());
					sb.Append(',');
				}
				if (sb.Length > 1)
					sb.Remove(sb.Length - 1, 1);
				sb.Append('}');
				return sb.ToString();
			}
			else
				throw new NotSupportedException("Unsupported value type: " + vt.FullName);
		}

		#endregion

		#region 集合成员

		public int Count
		{
            get
            {
                if (m_value is DList)
                    return ((DList)m_value).Count;
                else if (m_value is DMap)
                    return ((DMap)m_value).Count;
                else
                    throw new InvalidCastException();
            }
        }

		public DObject this[int index] => ((DList)m_value)[index];

		public DObject this[string key] => ((DMap)m_value)[key];

        public bool HasKey(string key)
        {
            var map = m_value as DMap;
            if (map == null)
                throw new Exception("NotJsonDictionaryNode");
            return map.ContainsKey(key);
        }

        public DObject GetNode(string key)
        {
            var map = m_value as DMap;
            if (map == null)
                throw new Exception("NotJsonDictionaryNode");

            DObject o;
            if (map.TryGetValue(key, out o))
                return o;
            return null;
        }

		#endregion

		#region JSON 互操作

		/// <summary>创建与给定的 JSON 对象对应的 DObject 对象</summary>
		/// <param name="jnode">JSON 对象</param>
		/// <returns>返回创建的 DObject 对象</returns>
		public static DObject ImportJson(JsonNode jnode)
		{
			switch (jnode.NodeType)
			{
				case JsonNodeType.Integer:
					return new DObject { m_value = jnode.IntValue };
				case JsonNodeType.Float:
					return new DObject { m_value = jnode.FloatValue };
				case JsonNodeType.Boolean:
					return new DObject { m_value = jnode.BoolValue };
				case JsonNodeType.String:
					return new DObject { m_value = jnode.TextValue };
				case JsonNodeType.Null:
					return new DObject { m_value = null };
				case JsonNodeType.NodeList:
					return ImportJsonList(jnode);
				case JsonNodeType.Dictionary:
					return ImportJsonDictionary(jnode);
				default:
					throw new InvalidCastException();
			}
		}

		static DObject ImportJsonList(JsonNode jnode)
		{
			var ls = new DList();
			foreach (var jni in jnode.ChildNodes)
				ls.Add(ImportJson(jni));
			return new DObject { m_value = ls };
		}

		static DObject ImportJsonDictionary(JsonNode jnode)
		{
			var dc = new DMap();
			foreach (var jni in jnode.ChildNodes)
				dc.Add(jni.Name, ImportJson(jni));
			return new DObject { m_value = dc };
		}

		/// <summary>创建与给定的 JSON 字符串对应的 DObject 对象</summary>
		/// <param name="str">JSON 字符串</param>
		/// <returns>返回创建的 DObject 对象</returns>
		public static DObject ImportJson(string str)
		{
			var json = JsonParser.ParseText(str);
			return ImportJson(json);
		}

		/// <summary>创建与给定的 DObject 对象对应的 JSON 对象</summary>
		/// <param name="o">DObject 对象</param>
		/// <returns>返回创建的 JSON 对象</returns>
		public static JsonNode ExportJson(DObject o)
		{
			object value = o.m_value;
			if (value == null)
				return new JsonNode(JsonNodeType.Null);

			var vt = value.GetType();
            if (vt == typeof(long))
                return new JsonNode((long)value);
            else if (vt == typeof(double))
                return new JsonNode((double)value);
            else if (vt == typeof(string))
                return new JsonNode((string)value);
            else if (vt == typeof(bool))
                return new JsonNode((bool)value);
            else if (vt == typeof(DList))
                return ExportJsonList((DList)value);
            else if (vt == typeof(DMap))
                return ExportJsonDictionary((DMap)value);
            else
                throw new InvalidCastException(vt.Name);
		}

		static JsonNode ExportJsonList(DList ls)
		{
			var jn = new JsonNode(JsonNodeType.NodeList);
			foreach (var o in ls)
				jn.AddChildItem(ExportJson(o));
			return jn;
		}

		static JsonNode ExportJsonDictionary(DMap map)
		{
			var jn = new JsonNode(JsonNodeType.Dictionary);
			foreach (var pair in map)
			{
				var jni = ExportJson(pair.Value);
				jni.Name = pair.Key;
				jn.AddChildItem(jni);
			}
			return jn;
		}

		/// <summary>返回给定 DObject 对象对应的 JSON 字符串</summary>
		/// <param name="o">DObject 对象</param>
		/// <returns>返回 JSON 字符串</returns>
		public static string ExportJsonStr(DObject o)
		{
			var jw = new JsonWriter();
			WriteJsonStr(jw, o, null);
			return jw.GetString();
		}

		static void WriteJsonStr(JsonWriter jw, DObject o, string name)
		{
			object value = o.m_value;
			if (value == null)
			{
				jw.WriteNull(name);
				return;
			}

			var vt = value.GetType();
            if (vt == typeof(long))
                jw.WriteInt(name, (long)value);
            else if (vt == typeof(double))
                jw.WriteFloat(name, (double)value);
            else if (vt == typeof(string))
                jw.WriteString(name, (string)value);
            else if (vt == typeof(bool))
                jw.WriteBool(name, (bool)value);
            else if (vt == typeof(DList))
            {
                jw.BeginList(name);
                foreach (var item in (DList)value)
                    WriteJsonStr(jw, item, null);
                jw.EndList();
            }
            else if (vt == typeof(DMap))
            {
                jw.BeginDictionary(name);
                foreach (var pair in (DMap)value)
                    WriteJsonStr(jw, pair.Value, pair.Key);
                jw.EndDictionary();
            }
            else
                throw new InvalidCastException(vt.Name);
		}

		#endregion
	}
}
