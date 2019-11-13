using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Nano.Collection;
using Nano.Json;
using Nano.Nuts;
using Puff.Marshal;

namespace Puff.NetCore
{
    public enum IceApiFlag
    {
        Json, JsonIn, Http
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IceApiAttribute : Attribute
    {
        public IceApiFlag Flags = IceApiFlag.Json;
        public string Ret = null;
        public string Stat = "stat";
        public string Cookie = null;
    }

    public interface IceApiRequest
    {
        object Raw { get; }
        string Url { get; }
        string Path { get; }
        string Method { get; }
        string QueryString { get; }
        string ContentType { get; }
        long? ContentLength { get; }
        IDictionary<string, string> Headers { get; }
        IDictionary<string, string> Cookies { get; }
        IDictionary<string, string> Query { get; }
        System.IO.Stream GetStream();
    }

    public class IceApiResponse
    {
        public const string MIME_Text = "text/plain";
        public const string MIME_Binary = "application/octet-stream";

        public const string CT_JsonUtf8 = "application/json; charset=utf-8";
        public const string CT_TextUtf8 = "text/plain; charset=utf-8";
        public const string CT_Binary = MIME_Binary;

        public int HttpStatusCode = 200;
        public string ContentType = null;
        public Dictionary<string, string> Headers = null;
        public JsonNode Json = null;
        public string Text = null;
        public byte[] Data = null;
        public Dictionary<string, string> Cookies = null;

        public void AddHeader(string key, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, string>();
            Headers.Add(key, value);
        }

        public void SetToSave(string name)
        {
            ContentType = IceApiResponse.CT_Binary;
            AddHeader("Content-Disposition", $"attachment; filename=\"{name}\"");
        }

        public static IceApiResponse Error(string text, string m = null)
        {
            var jn = new JsonNode(JsonNodeType.Dictionary);
            jn.AddChildItem(new JsonNode(text) { Name = "stat" });
            if (m != null && m.Length != 0)
                jn.AddChildItem(new JsonNode(m) { Name = "m" });
            return new IceApiResponse { HttpStatusCode = 200, Json = jn };
        }

        public static IceApiResponse String(string text) => new IceApiResponse { HttpStatusCode = 200, Text = text };
    }

    class MethodInBuilder
    {
        JsonObjectBuilder m_job;

        public MethodInBuilder(JsonObjectBuilder job) => m_job = job;

        public object[] PrepareJsonMethodArgs(MethodInfo m, JsonNode body, Dictionary<string, string> cookies)
        {
            var prms = m.GetParameters();
            var args = new object[prms.Length];
            for (int i = 0; i < prms.Length; ++i)
            {
                var prm = prms[i];
                args[i] = PrepareJsonMethodArg(prm.Name, prm.ParameterType, prm.RawDefaultValue, body, cookies);
            }
            return args;
        }

        public object PrepareJsonMethodArg(string name, Type vt, object defv, JsonNode body, Dictionary<string, string> cookies)
        {
            var jn = body.GetChildItem(name);
            if (jn != null)
                return m_job.Parse(vt, jn);

            string str;
            if (cookies != null && cookies.TryGetValue(name, out str))
                return Nano.Common.ExtConvert.FromString(vt, str);

            if (HasDefaultValue(defv))
                return defv;

            throw new NutsException("KeyNotFound:" + name);
        }

        public static bool HasDefaultValue(object v) => v == null || v.GetType() != typeof(DBNull);
    }

    class MethodOutBuilder2
    {
        JsonModelBuilder m_jmb;

        public MethodOutBuilder2(JsonModelBuilder jmb) => m_jmb = jmb;

        #region Json Style Return

        public IceApiResponse BuildJsonStyleApiReturn(JmMethod m, object o)
        {
            var jn = BuildJsonStyleReturn(m, o);
            var r = new IceApiResponse { Json = jn };
            r.Cookies = BuildJSR_Cookies(jn, m.Cookies);
            return r;
        }

        internal JsonNode BuildJsonStyleReturn(JmMethod m, object o)
        {
            var jn = BuildJSR_Main(o, m.Rets);
            BuildJSR_AddStat(jn, m.StatKey, "ok");
            return jn;
        }

        internal JsonNode BuildJSR_Main(object o, params string[] names)
        {
            if (names == null || names.Length == 0)
                return BuildJSR_NoRet(o);
            else if (names.Length == 1)
                return BuildJSR_OneRet(o, names);
            else
                return BuildJSR_MultiRet(o, names);
        }

        internal static void BuildJSR_AddStat(JsonNode jn, string key, string stat)
        {
            if (IsEmpty(key))
                return;

            var jni = CollectionKit.Find(jn.ChildNodes, x => x.Name == key);
            if (jni == null)
            {
                jni = new JsonNode(stat) { Name = key };
                jn.AddChildItem(jni);
            }
        }

        internal static Dictionary<string, string> BuildJSR_Cookies(JsonNode jn, params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                return null;

            var map = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                var jni = jn[key];
                var value = JsonToString(jni);
                map.Add(key, value);
            }
            return map;
        }

        #endregion

        #region Json Style Return - No Ret

        JsonNode BuildJSR_NoRet(object o)
        {
            if (o == null)  // 没有任何返回值
                return new JsonNode(JsonNodeType.Dictionary);
            else if (o is JsonNode)
            {
                var jn = (JsonNode)o;
                G.Verify(jn.NodeType == JsonNodeType.Dictionary, "WrongReturnType");
                return jn;
            }
            else if (o is DObject)
            {
                var jn = ((DObject)o).ToJson();
                G.Verify(jn.NodeType == JsonNodeType.Dictionary, "WrongReturnType");
                return jn;
            }
            else if (o is System.Collections.IDictionary)
                return BuildJSR_NoRet_Map((System.Collections.IDictionary)o);
            else
                return BuildJSR_NoRet_Class(o);
        }

        JsonNode BuildJSR_NoRet_Map(System.Collections.IDictionary map)
        {
            var jn = new JsonNode(JsonNodeType.Dictionary);
            foreach (System.Collections.DictionaryEntry e in map)
            {
                var jni = m_jmb.Build(e.Value);
                jni.Name = e.Key.ToString();
                jn.AddChildItem(jni);
            }
            return jn;
        }

        JsonNode BuildJSR_NoRet_Class(object o)
        {
            var jn = m_jmb.Build(o);
            G.Verify(jn.NodeType == JsonNodeType.Dictionary, "WrongReturnType");
            return jn;
        }

        #endregion

        #region Json Style Return - Ret Defined

        JsonNode BuildJSR_OneRet(object o, string[] names)
        {
            var jn = new JsonNode(JsonNodeType.Dictionary);
            var jni = m_jmb.Build(o);
            jni.Name = names[0];
            jn.AddChildItem(jni);
            return jn;
        }

        JsonNode BuildJSR_MultiRet(object o, string[] names)
        {
            if (o is ITuple)
                return BuildJSR_Ret_Tuple((ITuple)o, names);
            else if (o is System.Collections.IList)
                return BuildJSR_Ret_List((System.Collections.IList)o, names);
            else
                throw new NutsException("WrongReturnType");
        }

        JsonNode BuildJSR_Ret_Tuple(ITuple o, string[] names)
        {
            G.Verify(o.Length == names.Length, "WrongObjectNumber");
            var jn = new JsonNode(JsonNodeType.Dictionary);
            for (int i = 0; i < names.Length; ++i)
            {
                var jni = m_jmb.Build(o[i]);
                jni.Name = names[i];
                jn.AddChildItem(jni);
            }
            return jn;
        }

        JsonNode BuildJSR_Ret_List(System.Collections.IList o, string[] names)
        {
            G.Verify(o.Count == names.Length, "WrongObjectNumber");
            var jn = new JsonNode(JsonNodeType.Dictionary);
            for (int i = 0; i < names.Length; ++i)
            {
                var jni = m_jmb.Build(o[i]);
                jni.Name = names[i];
                jn.AddChildItem(jni);
            }
            return jn;
        }

        #endregion

        #region IceApiStyle.JsonIn

        #endregion

        #region Kits

        public static bool IsEmpty(string s) => s == null || s.Length == 0;

        public static string JsonToString(JsonNode jn)
        {
            switch (jn.NodeType)
            {
                case JsonNodeType.String:
                    return jn.TextValue;
                case JsonNodeType.Integer:
                    return jn.IntValue.ToString();
                case JsonNodeType.Float:
                    return jn.FloatValue.ToString();
                case JsonNodeType.Boolean:
                    return jn.BoolValue.ToString();
                default:
                    throw new NutsException("WrongJsonType");
            }
        }

        public static IList<string> RetrieveReturnValueTupleNames(MethodInfo mi)
        {
            G.Verify(mi.ReturnType.Name.StartsWith("ValueTuple`"), "Unexpected@RetrieveReturnValueTupleNames:Type)");
            var provider = mi.ReturnTypeCustomAttributes;
            var para = (ParameterInfo)provider;
            G.Verify(para.ParameterType.Name.StartsWith("ValueTuple`"), "Unexpected@RetrieveReturnValueTupleNames:Provider");
            var attr = para.GetCustomAttribute<TupleElementNamesAttribute>();
            return attr?.TransformNames;
        }

        #endregion
    }
}
