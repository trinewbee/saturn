using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nano.Json;
using Nano.Nuts;

namespace Puff.Marshal
{
    class ObTypeInfoCache
    {
        public class JmClass
        {
            public Type VT;
            public FieldInfo[] Fields;
            public PropertyInfo[] Props;
        }

        Dictionary<Type, JmClass> m_cmap = new Dictionary<Type, JmClass>();

        public JmClass RetrieveClassInfo(Type vt)
        {
            JmClass c;
            if (m_cmap.TryGetValue(vt, out c))
                return c;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            c = new JmClass { VT = vt };
            c.Fields = vt.GetFields(flags);
            c.Props = vt.GetProperties(flags);
            m_cmap.Add(vt, c);
            return c;
        }

        public JmClass RetrieveAnonymousClassInfo(Type vt)
        {
            JmClass c;
            if (m_cmap.TryGetValue(vt, out c))
                return c;

            c = new JmClass { VT = vt };
            c.Fields = new FieldInfo[0];
            c.Props = vt.GetProperties();
            m_cmap.Add(vt, c);
            return c;
        }
    }

    public interface IJobFilter
    {
        (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest);
    }

    public class JsonObjectBuilder : List<IJobFilter>
    {
        public const string WrongJsonType = "WrongJsonType";

        public object Parse(Type vt, JsonNode jn)
        {
            foreach (var filter in this)
            {
                var r = filter.TryParse(vt, jn, this);
                if (r.Item1)
                    return r.Item2;
            }
            throw new NutsException(NutsException.NotSupported, NutsException.NotSupported);
        }

        public static JsonObjectBuilder BuildDefault()
        {
            var ob = new JsonObjectBuilder
            {
                new JobFilterNull(),
                new JobFilterPrimitive(),
                new JobFilterEnum(),
                new JobFilterCommonGeneric(),
                new JobFilterArray(),
                new JobFilterClass()
            };
            return ob;
        }
    }

    public class JobFilterNull : IJobFilter
    {
        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (jn.NodeType == JsonNodeType.Null)
            {
                G.Verify(!vt.IsPrimitive, JsonObjectBuilder.WrongJsonType);
                return (true, null);
            }
            return (false, null);
        }
    }

    public class JobFilterPrimitive : IJobFilter
    {
        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (vt.IsPrimitive)
            {
                object o;
                if (jn.NodeType == JsonNodeType.String)
                    o = ConvertPrimitive(vt, jn.TextValue);
                else
                    o = ParsePrimitive(vt, jn);
                return (true, o);
            }
            else if (vt == typeof(string))
                return (true, jn.TextValue);
            else if (vt == typeof(JsonNode))
                return (true, jn);
            else if (vt == typeof(DObject))
                return (true, DObject.New(jn));
            else if (JmbForbidSystemTypes.IsForbid(vt))
                throw new NutsException("ForbidType:" + vt.Name, "ForbidType:" + vt.Name);
            else
                return (false, null);
        }

        public static double ToDouble(JsonNode jn) => jn.NodeType == JsonNodeType.Integer ? jn.IntValue : jn.FloatValue;

        public static char ToChar(string s)
        {
            G.Verify(s.Length == 1, NutsException.InvalidArg);
            return s[0];
        }

        public static object ParsePrimitive(Type vt, JsonNode jn)
        {
            switch (vt.FullName)
            {
                case "System.Int16":
                    return (short)jn.IntValue;
                case "System.UInt16":
                    return (ushort)jn.IntValue;
                case "System.Int32":
                    return (int)jn.IntValue;
                case "System.UInt32":
                    return (uint)jn.IntValue;
                case "System.Int64":
                    return jn.IntValue;
                case "System.UInt64":
                    return (ulong)jn.IntValue;
                case "System.Boolean":
                    return jn.BoolValue;
                case "System.Single":
                    return (float)ToDouble(jn);
                case "System.Double":
                    return ToDouble(jn);
                case "System.Byte":
                    return (byte)jn.IntValue;
                case "System.SByte":
                    return (sbyte)jn.IntValue;
                case "System.Char":
                    return (char)jn.IntValue;
                default:
                    throw new NutsException(NutsException.NotSupported, NutsException.NotSupported + ":" + vt.FullName);
            }
        }

        public static object ConvertPrimitive(Type vt, string s)
        {
            switch (vt.FullName)
            {
                case "System.Int16":
                    return Convert.ToInt16(s);
                case "System.UInt16":
                    return Convert.ToUInt16(s);
                case "System.Int32":
                    return Convert.ToInt32(s);
                case "System.UInt32":
                    return Convert.ToUInt32(s);
                case "System.Int64":
                    return Convert.ToInt64(s);
                case "System.UInt64":
                    return Convert.ToUInt64(s);
                case "System.Boolean":
                    return Convert.ToBoolean(s);
                case "System.Single":
                    return Convert.ToSingle(s);
                case "System.Double":
                    return Convert.ToDouble(s);
                case "System.Byte":
                    return Convert.ToByte(s);
                case "System.SByte":
                    return Convert.ToSByte(s);
                case "System.Char":
                    return ToChar(s);
                default:
                    throw new NutsException(NutsException.NotSupported, NutsException.NotSupported + ":" + vt.FullName);
            }
        }
    }

    public class JobFilterEnum : IJobFilter
    {
        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (!vt.IsEnum)
                return (false, null);

            if (jn.NodeType == JsonNodeType.String)
            {
                var e = Enum.Parse(vt, jn.TextValue);
                return (true, e);
            }
            else if (jn.NodeType == JsonNodeType.Integer)
            {
                var e = Enum.ToObject(vt, jn.IntValue);
                return (true, e);
            }
            else
                throw new NutsException(JsonObjectBuilder.WrongJsonType, JsonObjectBuilder.WrongJsonType + ":" + jn.NodeType);
        }
    }

    public class JobFilterCommonGeneric : IJobFilter
    {
        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (!vt.IsGenericType)
                return (false, null);

            var gt = vt.GetGenericTypeDefinition();
            if (gt == typeof(List<>))
            {
                var o = ParseList(vt, jn, nest);
                return (true, o);
            }
            else if (gt == typeof(Dictionary<,>))
            {
                var o = ParseDictionary(vt, jn, nest);
                return (true, o);
            }
            else if (gt == typeof(Nullable<>))
            {
                var o = ParseNullable(vt, jn);
                return (true, o);
            }
            else
                return (false, null);
        }

        public object ParseList(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            G.Verify(jn.NodeType == JsonNodeType.NodeList, JsonObjectBuilder.WrongJsonType);
            var pts = vt.GetGenericArguments();
            var it = pts[0];
            var ls = (System.Collections.IList)Activator.CreateInstance(vt);
            foreach (var jni in jn.ChildNodes)
            {
                var item = nest.Parse(it, jni);
                ls.Add(item);
            }
            return ls;
        }

        public object ParseDictionary(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            G.Verify(jn.NodeType == JsonNodeType.Dictionary, JsonObjectBuilder.WrongJsonType);
            var pts = vt.GetGenericArguments();
            G.Verify(pts[0] == typeof(string), JsonObjectBuilder.WrongJsonType);
            var it = pts[1];
            var dc = (System.Collections.IDictionary)Activator.CreateInstance(vt);
            foreach (var jni in jn.ChildNodes)
            {
                var item = nest.Parse(it, jni);
                dc.Add(jni.Name, item);
            }
            return dc;
        }

        public static object ParseNullable(Type vt, JsonNode jn)
        {
            bool isval = jn.NodeType != JsonNodeType.Null;
            var pt = vt.GetGenericArguments()[0];
            switch (pt.FullName)
            {
                case "System.Int16":
                    return isval ? (short?)jn.IntValue : null;
                case "System.UInt16":
                    return isval ? (ushort?)jn.IntValue : null;
                case "System.Int32":
                    return isval ? (int?)jn.IntValue : null;
                case "System.UInt32":
                    return isval ? (uint?)jn.IntValue : null;
                case "System.Int64":
                    return isval ? (long?)jn.IntValue : null;
                case "System.UInt64":
                    return isval ? (ulong?)jn.IntValue : null;
                case "System.Boolean":
                    return isval ? (bool?)jn.BoolValue : null;
                case "System.Single":
                    return isval ? (float?)JobFilterPrimitive.ToDouble(jn) : null;
                case "System.Double":
                    return isval ? (double?)JobFilterPrimitive.ToDouble(jn) : null;
                case "System.Byte":
                    return isval ? (byte?)jn.IntValue : null;
                case "System.SByte":
                    return isval ? (sbyte?)jn.IntValue : null;
                case "System.Char":
                    return isval ? (char?)jn.IntValue : null;
                default:
                    throw new NutsException(NutsException.NotSupported, NutsException.NotSupported + ":" + pt.FullName);
            }
        }
    }

    public class JobFilterArray : IJobFilter
    {
        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (vt.IsArray)
            {
                var o = ParseArray(vt, jn, nest);
                return (true, o);
            }
            return (false, null);
        }

        public static object ParseArray(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            G.Verify(jn.NodeType == JsonNodeType.NodeList, JsonObjectBuilder.WrongJsonType);
            var it = vt.GetElementType();
            var arr = Array.CreateInstance(it, jn.ChildCount);
            for (int i = 0; i < arr.Length; ++i)
            {
                var item = nest.Parse(it, jn[i]);
                arr.SetValue(item, i);
            }
            return arr;
        }
    }

    public class JobFilterClass : IJobFilter
    {
        ObTypeInfoCache m_cmap = new ObTypeInfoCache();

        public (bool, object) TryParse(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            if (vt.IsClass)
            {
                var o = ParseClass(vt, jn, nest);
                return (true, o);
            }
            return (false, null);
        }

        public object ParseClass(Type vt, JsonNode jn, JsonObjectBuilder nest)
        {
            G.Verify(jn.NodeType == JsonNodeType.Dictionary, JsonObjectBuilder.WrongJsonType);
            var c = m_cmap.RetrieveClassInfo(vt);
            var o = Activator.CreateInstance(vt);
            foreach (var field in c.Fields)
            {
                var jni = jn[field.Name];
                var oi = nest.Parse(field.FieldType, jni);
                field.SetValue(o, oi);
            }
            foreach (var prop in c.Props)
            {
                var jni = jn[prop.Name];
                var oi = nest.Parse(prop.PropertyType, jni);
                prop.SetValue(o, oi, null);
            }
            return o;
        }
    }
}
