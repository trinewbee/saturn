using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nano.Collection;
using Nano.Json;
using Nano.Nuts;
using Puff.Marshal;

namespace Puff.Model
{
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

    public class IceApiResponse
    {
        public int HttpStatusCode = 200;
        public JsonNode Json = null;
        public byte[] Data = null;
        public System.IO.Stream Stream = null;
        public Dictionary<string, string> Cookies = null;
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

        #region Notify

        public JsonNode BuildNotifyArgs(JmNotify notify, object[] args)
        {
            var prms = notify.MI.GetParameters();
            G.Verify(prms.Length == args.Length + 1, "InvalidArgNum");

            var jn = new JsonNode(JsonNodeType.Dictionary);
            var jni = new JsonNode(notify.Url) { Name = "sc:m" };
            jn.AddChildItem(jni);

            for (int i = 1; i < prms.Length; ++i)
            {
                jni = m_jmb.Build(args[i - 1]);
                jni.Name = prms[i].Name;
                jn.AddChildItem(jni);
            }
            return jn;
        }

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

    /*
    class MethodOutBuilder
	{
        JsonModelBuilder m_jmb = JsonModelBuilder.BuildDefault();

        // BuildNotifyArgs
        public JsonNode BuildObject(object o) => m_jmb.Build(o);

        public JsonNode BuildMethodReturn(JmMethod m, object ret)
        {
            if (m.Attr.CustomRet == true && m.Rets.Count() == 0)
                return m_jmb.Build(ret);
            else
                return BuildReturn(m.SuccCode, m.Rets, ret);
        }

        #region Return Tuple

        public JsonNode BuildReturn(string stat, string[] names, object o)
		{
			var jn = new JsonNode(JsonNodeType.Dictionary);
			jn.AddChildItem(new JsonNode(stat) { Name = "stat" });

			// Nuts 2.1
			if (o != null && o is DObject)
			{
				BuildReturnDObject(jn, names, (DObject)o);
				return jn;
			}

			if (names.Length > 1)
			{
				G.Verify(o != null, NutsException.InvalidArg);
				var vt = o.GetType();
				JmTuple jmt = RetrieveTupleInfo(vt);
				int pos = BuildTupleItems(jmt, jn, names, 0, o);
				G.Verify(names.Length == pos, "WrongItemNumber");
			}
			else if (names.Length == 1)
			{
				var jni = BuildObject(o);
				jni.Name = names[0];
				jn.AddChildItem(jni);
			}				
			return jn;
		}

		class JmTuple
		{
			public Type VT;
			public PropertyInfo[] Items;
			public PropertyInfo Rest;
		}

		Dictionary<Type, JmTuple> m_tmap = new Dictionary<Type, JmTuple>();

		JmTuple RetrieveTupleInfo(Type vt)
		{
			JmTuple jmt;
			if (m_tmap.TryGetValue(vt, out jmt))
				return jmt;

			jmt = new JmTuple { VT = vt };
			int n = GetTupleNumber(vt);
			G.Verify(n > 0, "NotTuple");
			G.Verify(n < 8, NutsException.OutOfRange);

			jmt.Items = GetTupleProps(vt, n < 8 ? n : 7);
			if (n == 8)
			{
				jmt.Rest = vt.GetProperty("TRest");
				G.Verify(jmt.Rest != null, NutsException.Unexpected);
			}

			m_tmap.Add(vt, jmt);
			return jmt;
		}

		int GetTupleNumber(Type vt)
		{
			const string prefix = "System.Tuple`";
			if (!vt.IsGenericType)				
				return -1;
			var gtn = vt.GetGenericTypeDefinition().FullName;
			if (!gtn.StartsWith(prefix))
				return -1;
			int n = int.Parse(gtn.Substring(prefix.Length));
			G.Verify(vt.GetGenericArguments().Length == n, NutsException.Unexpected);
			return n;
		}

		PropertyInfo[] GetTupleProps(Type vt, int n)
		{
			var props = new PropertyInfo[n];
			for (int i = 0; i < n; ++i)
			{
				props[i] = vt.GetProperty("Item" + (i + 1));
				G.Verify(props[i] != null, NutsException.Unexpected);
			}
			return props;
		}

		int BuildTupleItems(JmTuple jmt, JsonNode jn, string[] names, int pos, object o)
		{
			foreach (var prop in jmt.Items)
			{
				var oi = prop.GetValue(o, null);
				var jni = BuildObject(oi);
				jni.Name = names[pos++];
				jn.AddChildItem(jni);
			}
			if (jmt.Rest != null)
				throw new NutsException(NutsException.NotSupported);
			return pos;
		}

		void BuildReturnDObject(JsonNode jn, string[] names, DObject o)
		{
			if (names.Length > 1)
			{
				foreach (var name in names)
				{
					var jni = DObject.ExportJson(o[name]);
					jni.Name = name;
					jn.AddDictionaryItem(jni);
				}
			}
			else if (names.Length == 1)
			{
				var jni = DObject.ExportJson(o);
				jni.Name = names[0];
				jn.AddChildItem(jni);
			}
			else
				throw new NutsException("InvalidReturn");
		}

		#endregion
	}
    */
}
