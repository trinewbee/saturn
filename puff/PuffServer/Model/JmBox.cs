using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Nano.Collection;
using Nano.Json;
using Nano.Xml;
using Nano.Ext.CodeModel;
using Nano.Nuts;

namespace Puff.Model
{
	public enum IceApiFlag
	{
		Json, JsonIn, Http
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class IceServiceAttribute : Attribute
	{
		public string BaseUrl = null;
		public string SuccCode = "ok";
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class IceApiAttribute : Attribute
	{
		public IceApiFlag Flags = IceApiFlag.Json;
		public string Ret = null;
        public string Stat = "stat";
		public string Cookie = null;
        // public bool CustomRet = false;
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class IceNotifyAttribute : Attribute
	{
	}

	class JmMethod
	{
		public object Instance;
		public MethodInfo MI;
		public string Url;
		public IceApiAttribute Attr;
		public string SuccCode;
        public string StatKey;
		public string[] Rets;
		public string[] Cookies;

		public string Name => MI.Name;

        internal JmMethod() { } // for test

		public JmMethod(JmModule jmod, object instance, MethodInfo m)
		{
			Instance = instance;
			MI = m;
			Url = jmod.BaseUrl + '/' + m.Name.ToLowerInvariant();

			var attrs = m.GetCustomAttributes(typeof(IceApiAttribute), false);
			G.Verify(attrs.Length == 1, "ApiAttrNotFound");
			Attr = (IceApiAttribute)attrs[0];
			SuccCode = jmod.SuccCode;
            StatKey = Attr.Stat;

			Rets = Attr.Ret != null ? Attr.Ret.Split(',') : new string[0];
			CheckRet();
			Cookies = Attr.Cookie?.Split(',');            
		}

		public IceApiFlag Flags
		{
			get { return Attr.Flags; }
		}

		#region Initialize

		void CheckRet()
		{
			var vt = MI.ReturnType;
			if (Rets.Length > 1)
			{
				if (vt == typeof(object[]))
					return;

				int n = GetTupleCount(vt);
				G.Verify(Rets.Length == n, "WrongReturnItemNumber");
			}
            else if (vt.FullName.StartsWith("System.ValueTuple`"))
            {
                var names = MethodOutBuilder2.RetrieveReturnValueTupleNames(MI);
                G.Verify(names != null, "NameMissInValueTuple");
                Rets = names.ToArray();
            }
		}

		int GetTupleCount(Type vt)
		{
			const string prefix = "System.Tuple`";
			var gt = vt.GetGenericTypeDefinition();
			G.Verify(gt.FullName.StartsWith(prefix), "NotTupleType");

			// Nested tuple not supported
			return int.Parse(gt.FullName.Substring(prefix.Length));
		}

		#endregion
	}

	class JmNotify
	{
		public object Instance;
		public string Url;
		public FieldInfo FI;
		public IceNotifyAttribute Attr;
		public MethodInfo MI;   // invoke method

		public string Name => FI.Name;

		public JmNotify(JmModule jmod, object instance, FieldInfo field)
		{
			Instance = instance;
			Url = jmod.BaseUrl + '/' + field.Name.ToLowerInvariant();
			FI = field;

			var attrs = field.GetCustomAttributes(typeof(IceNotifyAttribute), false);
			G.Verify(attrs.Length == 1, "NotifyAttrNotFound");
			Attr = (IceNotifyAttribute)attrs[0];

			var vt = field.FieldType;
			Debug.Assert(vt.BaseType == typeof(MulticastDelegate));
			MI = vt.GetMethod("Invoke");
			Debug.Assert(MI != null);
		}

		public void LinkNotify(object oTag, MethodInfo miTag)
		{
			var o = BuildNotifyDelegate(FI.FieldType, oTag, miTag, this);
			FI.SetValue(Instance, o);
		}

		static object BuildNotifyDelegate(Type dt, object oTag, MethodInfo miTag, JmNotify oNotify)
		{
			var argx = LEM.Args(dt);
			Debug.Assert(argx[0].Type == typeof(List<long>));

			// delegate := (List<int> uids, params object[] args)
			// invoke := (List<int> uids, JmNotifyBox notify, params object[] args)
			var eprms = LEM.ObjectArray(argx.Items, 1, argx.Count - 1);
			var e = LEM.Value(oTag).Call(miTag, argx[0], LEM.Value(oNotify), eprms);
			return e.Compile(dt, argx);
		}
	}

	class JmModule
	{
		public IceServiceAttribute Attr;
		public object Instance;
		public string BaseUrl;
		public string SuccCode;
		public Dictionary<string, JmMethod> Methods;
		public Dictionary<string, JmNotify> Notifies;

		public void Init(object o)
		{
			G.Verify(Methods == null, NutsException.AlreadyOpen);

			Instance = o;
			var vt = o.GetType();
			var attrs = vt.GetCustomAttributes(typeof(IceServiceAttribute), false);
			G.Verify(attrs.Length == 1, "ServiceAttrNotFound");
			var Attr = (IceServiceAttribute)attrs[0];
			G.Verify(Attr.BaseUrl != null && Attr.BaseUrl[0] == '/' && Attr.BaseUrl[Attr.BaseUrl.Length - 1] != '/', "WrongBaseUrl");
			BaseUrl = Attr.BaseUrl.ToLowerInvariant();
			SuccCode = Attr.SuccCode;

			Methods = new Dictionary<string, JmMethod>();
			var methods = vt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var method in methods)
			{
				attrs = method.GetCustomAttributes(typeof(IceApiAttribute), false);
				if (attrs.Length == 0)
					continue;

				G.Verify(attrs.Length == 1, NutsException.TooManyResults);
				var m = new JmMethod(this, o, method);
				Methods.Add(m.Url, m);
			}

			Notifies = new Dictionary<string, JmNotify>();
			var fields = vt.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var field in fields)
			{
				attrs = field.GetCustomAttributes(typeof(IceNotifyAttribute), false);
				if (attrs.Length == 0)
					continue;

				G.Verify(attrs.Length == 1, NutsException.TooManyResults);
				var n = new JmNotify(this, o, field);
				Notifies.Add(n.Url, n);
			}
		}

		public JmMethod GetMethod(string url)
		{
			JmMethod m;
			if (Methods.TryGetValue(url.ToLowerInvariant(), out m))
				return m;
			else
				return null;
		}
	}

	class JmModel
	{
		public delegate void JmNotifyDelegate(List<int> uids, string method, JsonNode jnode);

		public Dictionary<string, JmModule> Services = new Dictionary<string, JmModule>();

		public JmModule AddService(object o)
		{
			var service = new JmModule();
			service.Init(o);
			Services.Add(service.BaseUrl, service);
			return service;
		}
	}

	class JmDump
	{
		HashSet<Type> m_vtmsg = new HashSet<Type>();

		static HashSet<Type> m_vtdir;

		static JmDump()
		{
			m_vtdir = new HashSet<Type>();
			m_vtdir.Add(typeof(void));
			m_vtdir.Add(typeof(string));
			m_vtdir.Add(typeof(JsonNode));
		}

		#region Inspect messenger types

		void InspectService(JmModule jms)
		{
			foreach (var m in jms.Methods.Values)
			{
				var mi = m.MI;
				foreach (var prm in mi.GetParameters())
					InspectType(prm.ParameterType);
				InspectType(mi.ReturnType);
			}
		}

		void InspectType(Type vt)
		{
			if (vt.IsPrimitive || m_vtdir.Contains(vt))
				return;
			else if (vt.IsGenericType)
				InspectGenericType(vt);
			else if (vt.IsArray)
				InspectType(vt.GetElementType());
			else if (vt.IsClass)
				InspectClassType(vt);
			else
				throw new NutsException(NutsException.NotSupported);
		}

		void InspectGenericType(Type vt)
		{
			var gt = vt.GetGenericTypeDefinition();
			var pts = vt.GetGenericArguments();
			if (gt == typeof(List<>))
			{
				InspectType(pts[0]);
			}
			else if (gt == typeof(Dictionary<,>))
			{
				G.Verify(pts[0] == typeof(string), NutsException.NotSupported);
				InspectType(pts[1]);
			}
			else if (gt.FullName.StartsWith("System.Tuple`"))
			{
				foreach (var pt in vt.GetGenericArguments())
					InspectType(pt);
			}
			else
				throw new NutsException(NutsException.NotSupported);
		}

		void InspectClassType(Type vt)
		{
			if (m_vtmsg.Contains(vt))
				return;

			m_vtmsg.Add(vt);
			foreach (var field in vt.GetFields())
				InspectType(field.FieldType);
		}

		#endregion

		public void Dump(JmModel jmm, string path)
		{
			using (var wr = XmlKit.CreateXmlWriter(path))
				Dump(jmm, wr);
		}

		public void Dump(JmModel jmm, XmlWriter tw)
		{
			foreach (var jms in jmm.Services.Values)
				InspectService(jms);

			tw.WriteStartDocument();
			tw.WriteStartElement("JModel");

			tw.WriteStartElement("Objects");
			var vts = CollectionKit.ToList(m_vtmsg);
			vts.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			foreach (var vt in vts)
				DumpMsg(tw, vt, "i");
			tw.WriteEndElement();

			tw.WriteStartElement("Services");
			foreach (var jms in jmm.Services.Values)
				DumpService(tw, jms, "i");
			tw.WriteEndElement();

			tw.WriteEndElement();
			tw.WriteEndDocument();
			tw.Close();
		}

		void DumpMsg(XmlWriter tw, Type vt, string name)
		{
			tw.WriteStartElement(name);
			tw.WriteAttributeString("Name", GetTypeName(vt));

			tw.WriteStartElement("Fields");
			foreach (var f in vt.GetFields())
			{
				tw.WriteStartElement("i");
				tw.WriteAttributeString("Name", f.Name);
				tw.WriteAttributeString("VT", GetTypeName(f.FieldType));
				tw.WriteEndElement();
			}
			tw.WriteEndElement();

			tw.WriteEndElement();
		}

		void DumpService(XmlWriter tw, JmModule box, string name)
		{
			tw.WriteStartElement(name);
			tw.WriteAttributeString("Name", GetTypeName(box.Instance.GetType()));
			tw.WriteAttributeString("BaseUrl", box.BaseUrl);

			tw.WriteStartElement("Apis");
			var methods = CollectionKit.ToList(box.Methods.Values);
			methods.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			foreach (var m in methods)
				DumpMethod(tw, m, "i");
			tw.WriteEndElement();

			tw.WriteEndElement();
		}

		void DumpMethod(XmlWriter tw, JmMethod m, string name)
		{
			tw.WriteStartElement(name);
			tw.WriteAttributeString("Name", m.Name);

			tw.WriteStartElement("InArgs");
			foreach (var prm in m.MI.GetParameters())
				DumpMethodArg(tw, prm.Name, prm.ParameterType, "i");
			tw.WriteEndElement();

			tw.WriteStartElement("OutArgs");
			if (m.Rets != null && m.Rets.Length > 0)
				DumpMethodRet(tw, m, name);
			tw.WriteEndElement();

			tw.WriteEndElement();
		}

		void DumpMethodArg(XmlWriter tw, string vname, Type vt, string name)
		{
			tw.WriteStartElement(name);
			tw.WriteAttributeString("Name", vname);
			tw.WriteAttributeString("VT", GetTypeName(vt));
			tw.WriteEndElement();
		}

		void DumpMethodRet(XmlWriter tw, JmMethod m, string name)
		{
			var rets = m.Rets;
			var vt = m.MI.ReturnType;
			if (rets.Length > 1)
			{
				G.Verify(vt.FullName.StartsWith("System.Tuple`"), NutsException.InvalidArg);
				var pts = vt.GetGenericArguments();
				G.Verify(pts.Length == rets.Length, NutsException.InvalidArg);
				for (int i = 0; i < rets.Length; ++i)
				{
					tw.WriteStartElement(name);
					tw.WriteAttributeString("Name", rets[i]);
					tw.WriteAttributeString("VT", GetTypeName(pts[i]));
					tw.WriteEndElement();
				}
			}
			else
			{
				tw.WriteStartElement(name);
				tw.WriteAttributeString("Name", rets[0]);
				tw.WriteAttributeString("VT", GetTypeName(vt));
				tw.WriteEndElement();
			}
		}

		static string GetTypeName(Type vt)
		{
			if (vt.IsGenericType)
			{
				var gt = vt.GetGenericTypeDefinition();
				var sb = new StringBuilder(gt.FullName);
				sb.Append('[');
				foreach (var pt in vt.GetGenericArguments())
					sb.Append(GetTypeName(pt)).Append(',');
				sb[sb.Length - 1] = ']';
				return sb.ToString();
			}
			else
				return vt.FullName;
		}
	}
}
