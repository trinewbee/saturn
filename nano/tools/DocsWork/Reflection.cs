using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Nano.Collection;

namespace DocsWork
{
	class AsmMetaParser
	{
		public AsmxAssembly Asmx { get; private set; }

		public void Load(string path)
		{
			// Assembly asm = Assembly.ReflectionOnlyLoadFrom(path);
			Assembly asm = Assembly.LoadFrom(path);
			string name = asm.GetName().Name;
			Asmx = new AsmxAssembly() { Name = name };

			var types = asm.GetTypes();
			foreach (var type in types)
			{
				if (type.IsNestedPrivate || type.Name == "<PrivateImplementationDetails>")
					continue;
				LoadType(type);
			}

			SortTypeMembers();
		}

		#region Type

		void LoadType(Type type)
		{
			string name = type.FullName;
			var ns = Asmx.ValidateNamespace(type.Namespace);

			var ti = new AsmxType() { Name = name, NS = ns };
			LoadTypeAttr(ti, type);
			ns.Tis.Add(ti.Name, ti);

			var fields = type.GetFields();
			foreach (var field in fields)
				LoadField(ti, field);

			var props = type.GetProperties();
			foreach (var prop in props)
				LoadProperty(ti, prop);

			var methods = type.GetMethods();
			foreach (var method in methods)
				LoadMethod(ti, method);
		}

		void LoadTypeAttr(AsmxType ti, Type type)
		{
			ti.IsVisible = type.IsVisible;
			ti.Display = AsmTypeKit.GetDisplayName(type);
			ti.Refd = AsmTypeKit.GetTypeRefdName(type);
			ti.TClass = GetTypeClass(type);
			ti.Attributes = type.Attributes;
			ti.BaseType = type.BaseType != null ? type.BaseType.FullName : null;
			if (ti.BaseType == "System.Object")
				ti.BaseType = null;
			else if (ti.BaseType == "System.MulticastDelegate")
				ti.TClass = AsmxTypeClass.Delegate;

			if (type.IsNested)
			{
				string name = type.FullName;
				int pos = name.LastIndexOf('+');
				ti.NestOwner = name.Substring(0, pos);
			}

			var tys = type.GetInterfaces();
			ti.Interfaces = CollectionKit.Transform(tys, x => x.FullName, tys.Length);
		}

		static AsmxTypeClass GetTypeClass(Type type)
		{
			if (type.IsClass)
				return AsmxTypeClass.Class;
			else if (type.IsInterface)
				return AsmxTypeClass.Interface;
			else if (type.IsEnum)
				return AsmxTypeClass.Enum;
			else if (type.IsValueType)
				return AsmxTypeClass.Struct;
			else
				throw new NotSupportedException("Unknown type class: " + type.FullName);
		}

		#endregion

		void LoadField(AsmxType ti, FieldInfo field)
		{
			var fi = new AsmxField() { Name = field.Name, Ti = ti };
			fi.ValueType = AsmTypeKit.GetDisplayName(field.FieldType);
			ti.Fields.Add(fi);
		}

		void LoadProperty(AsmxType ti, PropertyInfo prop)
		{
			var pi = new AsmxProperty() { Name = prop.Name, Ti = ti };
			pi.ValueType = AsmTypeKit.GetDisplayName(prop.PropertyType);
			pi.CanRead = prop.CanRead;
			pi.CanWrite = prop.CanWrite;
			ti.Props.Add(pi);
		}

		void LoadMethod(AsmxType ti, MethodInfo method)
		{
			var mi = new AsmxMethod() { Name = method.Name, Ti = ti };
			mi.RefdName = AsmTypeKit.GetMethodRefdName(method);
			mi.ReturnType = AsmTypeKit.GetDisplayName(method.ReturnType);
			mi.RefdSig = LoadMethodArgs(method, out mi.Parameters);

			ti.Methods.Add(mi);
		}

		string LoadMethodArgs(MethodInfo method, out AsmxParameter[] prmxs)
		{
			ParameterInfo[] prms = method.GetParameters();
			var sbSig = new StringBuilder();
			prmxs = new AsmxParameter[prms.Length];
			for (int i = 0; i < prms.Length; ++i)
			{
				var prm = prms[i];
				prmxs[i] = ParseParameter(prm);

				if (i != 0)
					sbSig.Append(',');
				string refd = AsmTypeKit.GetArgRefdName(prm.ParameterType, method);
				sbSig.Append(refd);
			}
			return sbSig.ToString();
		}

		AsmxParameter ParseParameter(ParameterInfo prm)
		{
			var prmx = new AsmxParameter();
			prmx.Name = prm.Name;
			var vty = prm.ParameterType;
			prmx.ValueType = AsmTypeKit.GetDisplayName(vty);
			if (vty.IsByRef)
			{
				Debug.Assert(vty.Name.EndsWith("&"));
				prmx.Flags = prm.IsOut ? AsmxParameter.Flag.OutArg : AsmxParameter.Flag.RefArg;
			}
			if (prm.IsOptional)
			{
				prmx.Flags |= AsmxParameter.Flag.Optional;
				prmx.Defv = prm.DefaultValue;
			}
			return prmx;
		}

		void SortTypeMembers()
		{
			foreach (var ns in Asmx.Nss.Values)
			{
				foreach (var ti in ns.Tis.Values)
					ti.SortMembers();
			}
		}
	}

	static class AsmTypeKit
	{
		#region Constructor

		static Dictionary<Type, string> m_bultinTypes;
		static char[] m_digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		static AsmTypeKit()
		{
			m_bultinTypes = new Dictionary<Type, string>
			{
				{ typeof(void), "void" },
				{ typeof(object), "object" },

				{ typeof(byte), "byte" },
				{ typeof(sbyte), "sbyte" },
				{ typeof(short), "short" },
				{ typeof(ushort), "ushort" },
				{ typeof(int), "int" },
				{ typeof(uint), "uint" },
				{ typeof(long), "long" },
				{ typeof(ulong), "ulong" },

				{ typeof(float), "float" },
				{ typeof(double), "double" },
				{ typeof(bool), "bool" },

				{ typeof(char), "char" },
				{ typeof(string), "string" },
			};
		}

		#endregion

		#region Get type friendly name

		public static string GetDisplayName(Type type)
		{
			if (type == null)
				return null;

			if (type.FullName == null)
			{
				if (type.IsGenericParameter)
				{
					Debug.Assert(type.FullName == null);
					return type.Name;
				}
				else if (type.BaseType == typeof(System.MulticastDelegate))
				{
					return GetDelegateDisplayName(type);
				}
				else if (type.IsGenericType)
				{
					string name = type.Name;
					name = ExpandGenericDisplayName(name, type);
					return name;
				}
				else if (type.BaseType == typeof(System.Array))
				{
					return type.Name;
				}
				else if (type.IsByRef)
				{
					// bool BomDictionary<KT, VT>.TryGetValue(KT key, out VT value) -> VT&
					Debug.Assert(type.Name.EndsWith("&"));
					return type.Name.Substring(0, type.Name.Length - 1);
				}
				else
					throw new NotSupportedException();
			}
			else
			{
				string name = GetBuiltinDisplayName(type);
				if (name != null)
					return name;

				if (type.IsArray)
				{
					return GetArrayDisplayName(type);
				}
				else if (!type.IsNested)
				{
					// normal classes: A.B.C
					// normal generic classes: A.B.C`1[[very long]]
					name = type.FullName;
					int epos = name.IndexOf('`');
					if (epos < 0)
						epos = name.Length;

					int pos = name.LastIndexOf('.', epos - 1);
					name = name.Substring(pos + 1);

					if (type.IsGenericType)
						name = ExpandGenericDisplayName(name, type);
				}
				else
				{
					// nested classes: X+Y
					// nested generic classes: X`1+Y`1

					Type oty = type.DeclaringType;
					string prefix = GetDisplayName(oty);

					name = type.Name;
					if (type.IsGenericType)
						name = ExpandGenericDisplayName(name, type);

					return prefix + '.' + name;
				}

				return name;
			}
		}

		static string GetArrayDisplayName(Type type)
		{
			// Hack
			var m = type.GetMethod("Get");
			Debug.Assert(m != null);
			string name = GetDisplayName(m.ReturnType);
			return name + "[]";
		}

		static string GetDelegateDisplayName(Type type)
		{
			string name = type.Name;
			if (type.IsGenericType)
				name = ExpandGenericDisplayName(name, type);
			return name;
		}

		static string GetBuiltinDisplayName(Type type)
		{
			string name;
			if (m_bultinTypes.TryGetValue(type, out name))
				return name;
			return null;
		}

		static char[] spchrs = new char[] { '.', '+' };

		static string ExpandGenericDisplayName(string name, Type type)
		{
			// System.Collections.Generic.IEnumerable`1[[Nano.Storage.ObjectInfo, Nano.Common, Version=1.2.6065.26530, Culture=neutral, PublicKeyToken=null]]

			int pos = name.IndexOf('`');
			if (pos < 0)
				return name;

			var vts = type.GetGenericArguments();
			var sb = new StringBuilder();
			sb.Append(name.Substring(0, pos)).Append('<');
			foreach (var vt in vts)
			{
				string subname = GetDisplayName(vt);
				sb.Append(subname).Append(',');
			}
			sb[sb.Length - 1] = '>';
			
			return sb.ToString();
		}

		#endregion

		#region Get xml documented name

		public static string GetTypeRefdName(Type type)
		{
			Debug.Assert(type.FullName != null);
			return GetRefdNameFN(type);
		}

		static string GetRefdNameFN(Type type)
		{
			if (type.IsNested)
			{
				Type oty = type.DeclaringType;
				string prefix = GetTypeRefdName(oty);

				string name = type.Name;
				if (type.IsGenericType)
					name = GetGenericRefdName(name);

				return prefix + '.' + name;
			}
			else
			{
				string name = type.FullName;
				if (type.IsGenericType)
					name = GetGenericRefdName(name);
				return name;
			}
		}

		static string GetGenericRefdName(string name)
		{
			int pos = name.IndexOf('`');
			if (pos < 0)
				return name;

			bool refMark = name.EndsWith("&");
			++pos;
			while (pos < name.Length)
			{
				char ch = name[pos];
				if (ch < '0' || ch > '9')
					break;
				++pos;
			}
			name = name.Substring(0, pos);
			if (refMark)
				name += '&';
			return name;
		}

		public static string GetMethodRefdName(MethodInfo m)
		{
			if (m.IsGenericMethod)
			{
				var vtys = m.GetGenericArguments();
				Debug.Assert(vtys.Length > 0);
				return m.Name + "``" + vtys.Length;
			}
			else
				return m.Name;
		}

		public static string GetArgRefdName(Type type, MethodInfo m)
		{
			string name;
			if (type.FullName != null)
				name = GetRefdNameFN(type);
			else
				name = GetArgRefdNameSN(type, m);

			if (name[name.Length - 1] == '&')
				name = name.Substring(0, name.Length - 1) + '@';
			return name;
		}

		static string GetArgRefdNameSN(Type type, MethodInfo m)
		{
			// M:Nano.Collection.CollectionKit.WalkGeneral``1(System.Collections.Generic.IEnumerable{``0},System.Action{``0})
			if (type.IsGenericParameter)
			{
				// M:Nano.Xml.IXmlNodeAccept`1.BuildFromNode(`0,System.Xml.XmlElement)
				return GetArgRefdGenericPrmName(type);
			}
			else if (type.IsGenericType)
			{
				// M:Nano.Xml.XmlKit.LoadTree``1(System.Xml.XmlElement,``0,Nano.Xml.IXmlNodeAccept{``0})
				string name = type.Name;
				int pos = name.LastIndexOf('`');
				name = name.Substring(0, pos);
				var sb = new StringBuilder();
				sb.Append(type.Namespace).Append('.').Append(name);

				var vtys = type.GetGenericArguments();
				sb.Append('{');
				foreach (var vty in vtys)
				{
					name = GetArgRefdName(vty, m);
					sb.Append(name).Append(',');
				}
				sb[sb.Length - 1] = '}';
				return sb.ToString();
			}
			else if (type.BaseType == typeof(System.Array))
			{
				// M:Nano.Collection.CollectionKit.ToArray``1(``0[],System.Int32,System.Int32)
				// Hacking
				Debug.Assert(type.Name.EndsWith("[]"));
				string name = type.Name;
				name = name.Substring(0, name.Length - 2);
				return HackFindGenericPrmName(name, m) + "[]";
			}
			else if (type.IsByRef)
			{
				// M:Nano.Ext.Persist.BomDictionary`2.TryGetValue(`0,`1@)
				Debug.Assert(type.Name.EndsWith("&"));
				string name = type.Name;
				name = name.Substring(0, name.Length - 1);
				return HackFindGenericPrmName(name, m) + '&';
			}
			else
			{
				Debug.Fail("Unsupported");
				throw new NotSupportedException();
			}
		}

		static string GetArgRefdGenericPrmName(Type type)
		{
			Debug.Assert(type.IsGenericParameter);
			string index = type.GenericParameterPosition.ToString();
			if (type.DeclaringMethod != null)
				return "``" + index;
			else
				return "`" + index;
		}

		static string HackFindGenericPrmName(string name, MethodInfo m)
		{
			Type oty = m.DeclaringType;
			var vtys = oty.GetGenericArguments();
			for (int i = 0; i < vtys.Length; ++i)
			{
				if (vtys[i].Name == name)
					return '`' + i.ToString();
			}
			vtys = m.GetGenericArguments();
			for (int i = 0; i < vtys.Length; ++i)
			{
				if (vtys[i].Name == name)
					return "``" + i.ToString();
			}

			Debug.Fail("Unsupported");
			throw new NotSupportedException();
		}

		#endregion
	}
}
