using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nano.Json;
using Nano.Nuts;

namespace Puff.Marshal
{
    public interface IJmbFilter
    {
        JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest);
    }

    public class JsonModelBuilder : List<IJmbFilter>
    {
        public JsonNode TryBuild(object o)
        {
            if (o == null)
                return new JsonNode(JsonNodeType.Null);

            var vt = o.GetType();
            foreach (var filter in this)
            {
                var jn = filter.TryBuild(vt, o, this);
                if (jn != null)
                    return jn;
            }

            return null;
        }

        public JsonNode Build(object o)
        {
            var jn = TryBuild(o);
            if (jn == null)
                throw new NutsException(NutsException.NotSupported);
            return jn;
        }

        public static JsonModelBuilder BuildDefault()
        {
            var jmb = new JsonModelBuilder
            {
                new JmbFilterPrimitive(),
                new JmbFilterCommonGeneric(),
                new JmbFilterArray(),
                new JmbFilterEnum(),
                new JmbFilterClass()
            };
            return jmb;
        }
    }

    public static class JmbForbidSystemTypes
    {
        public static HashSet<string> ForbidTypes = new HashSet<string>
        {
            "System.DateTime"
        };

        public static bool IsForbid(Type vt) => ForbidTypes.Contains(vt.FullName);
    }

    public class JmbFilterPrimitive : IJmbFilter
    {
        public JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest)
        {
            if (vt.IsPrimitive)
                return BuildPrimitive(vt, o);
            else if (vt == typeof(string))
                return new JsonNode((string)o);
            else if (vt == typeof(JsonNode))
                return (JsonNode)o;
            else if (vt == typeof(DObject))
                return DObject.ExportJson((DObject)o);
            else if (JmbForbidSystemTypes.IsForbid(vt))
                throw new NutsException("ForbidType:" + vt.Name);
            else
                return null;
        }

        public static JsonNode BuildPrimitive(Type vt, object o)
        {
            switch (vt.FullName)
            {
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
                case "System.Byte":
                    return new JsonNode((byte)o);
                case "System.SByte":
                    return new JsonNode((sbyte)o);
                case "System.Char":
                    return new JsonNode((char)o);
                default:
                    throw new NutsException(NutsException.NotSupported);
            }
        }
    }

    public class JmbFilterCommonGeneric : IJmbFilter
    {
        public static Type TY_List = typeof(System.Collections.IList);
        public static Type TY_Map = typeof(System.Collections.IDictionary);

        public JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest)
        {
            if (TY_List.IsInstanceOfType(o))
                return BuildList((System.Collections.IList)o, nest);
            else if (TY_Map.IsInstanceOfType(o))
                return BuildDictionary((System.Collections.IDictionary)o, nest);
            else
                return null;
        }

        public static JsonNode BuildList(System.Collections.IList ls, JsonModelBuilder nest)
        {
            var jnode = new JsonNode(JsonNodeType.NodeList);
            foreach (var item in ls)
            {
                var jnsub = nest.Build(item);
                jnode.AddChildItem(jnsub);
            }
            return jnode;
        }

        public static JsonNode BuildDictionary(System.Collections.IDictionary o, JsonModelBuilder nest)
        {
            var dc = (System.Collections.IDictionary)o;
            var jnode = new JsonNode(JsonNodeType.Dictionary);
            foreach (System.Collections.DictionaryEntry pair in dc)
            {
                var jnsub = nest.Build(pair.Value);
                jnsub.Name = pair.Key.ToString();
                jnode.AddChildItem(jnsub);
            }
            return jnode;
        }
    }

    public class JmbFilterArray : IJmbFilter
    {
        public JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest)
        {
            if (vt.IsArray)
                return BuildArray((Array)o, nest);
            return null;
        }

        public static JsonNode BuildArray(Array arr, JsonModelBuilder nest)
        {
            G.Verify(arr.Rank == 1, NutsException.NotSupported);
            var jnode = new JsonNode(JsonNodeType.NodeList);
            foreach (var item in arr)
            {
                var jnsub = nest.Build(item);
                jnode.AddChildItem(jnsub);
            }
            return jnode;
        }
    }

    public class JmbFilterEnum : IJmbFilter
    {
        class EnumInfo
        {
            public Type VT;
            public System.Reflection.FieldInfo F_Value;
        }

        Dictionary<string, EnumInfo> map = new Dictionary<string, EnumInfo>();

        public JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest)
        {
            if (vt.IsEnum)
            {
                var info = ValidateEnumInfo(vt);
                var i = (int)info.F_Value.GetValue(o);
                return new JsonNode(i);
            }
            return null;
        }

        EnumInfo ValidateEnumInfo(Type vt)
        {
            var name = vt.FullName;
            EnumInfo info;
            if (map.TryGetValue(name, out info))
                return info;

            info = new EnumInfo { VT = vt };
            info.F_Value = vt.GetField("value__");
            G.Verify(info.F_Value.FieldType == typeof(int), "UnsupportEnumValueField");
            map.Add(name, info);
            return info;
        }

        public static int GetEnumValue(Type vt, object o)
        {
            var field = vt.GetField("value__");
            return (int)field.GetValue(o);
        }
    }

    public class JmbFilterClass : IJmbFilter
    {
        ObTypeInfoCache m_cmap = new ObTypeInfoCache();

        public JsonNode TryBuild(Type vt, object o, JsonModelBuilder nest)
        {
            if (DObject.IsCompilerGenerated(vt))
                return BuildClass(vt, o, nest, m_cmap.RetrieveAnonymousClassInfo);
            else
                return BuildClass(vt, o, nest, m_cmap.RetrieveClassInfo);
        }

        static JsonNode BuildClass(Type vt, object o, JsonModelBuilder nest, Func<Type, ObTypeInfoCache.JmClass> f)
        {
            var c = f(vt);
            var jn = new JsonNode(JsonNodeType.Dictionary);
            foreach (var field in c.Fields)
            {
                string name = field.Name;
                object value = field.GetValue(o);
                var jni = nest.Build(value);
                jni.Name = name;
                jn.AddChildItem(jni);
            }
            foreach (var prop in c.Props)
            {
                string name = prop.Name;
                object value = prop.GetValue(o, null);
                var jni = nest.Build(value);
                jni.Name = name;
                jn.AddChildItem(jni);
            }
            return jn;
        }
    }
}
