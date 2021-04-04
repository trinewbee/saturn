using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;

namespace Nano.Ext.Persist
{
	public enum CustomNodeMode
	{
		Ignore, Attr, Element
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class CustomNodeAttribute : Attribute
	{
		public CustomNodeMode Mode;
		public string Loader, Saver;
	}

	public static class SimpleXmlLoader
	{
		public static object LoadXmlDocument(string path, Type vt)
		{
			var doc = new XmlDocument();
			doc.Load(path);
			return ParseElement(vt, doc.DocumentElement);
		}

		public static object LoadXmlDocument(Stream stream, Type vt)
        {
			var doc = new XmlDocument();
			doc.Load(stream);
			return ParseElement(vt, doc.DocumentElement);
        }

		public static T LoadXmlDocument<T>(string path) => (T)LoadXmlDocument(path, typeof(T));

		public static T LoadXmlDocument<T>(Stream stream) => (T)LoadXmlDocument(stream, typeof(T));

		#region Element Values

		public static object ParseElement(Type vt, XmlElement e)
		{
			var attr = e.GetAttributeNode("_null");
			if (attr != null && attr.Value != "0")
				return null;

			if (vt.IsValueType || vt == typeof(string))
				return ParseValueElement(vt, e);
			else if (vt.IsArray)
				return ParseArrayElement(vt, e);
			else if (vt.IsGenericType)
				return ParseGenericElement(vt, e);
			else if (vt.IsClass)
				return ParseObjectElement(vt, e);
			else
				throw new NotSupportedException("Unsupported type: " + vt.FullName);
		}

		public static T ParseElement<T>(XmlElement e) => (T)ParseElement(typeof(T), e);

		static object ParseValueElement(Type vt, XmlElement e)
		{
			string s = e.GetAttribute("value");
			if (vt == typeof(int))
				return int.Parse(s);
			else if (vt == typeof(string))
				return s;
			else if (vt.IsGenericType)
				return ParseGenericValueElement(vt, e);
			else
				throw new NotSupportedException("Unsupported value type: " + vt.FullName);
		}

		static object ParseGenericValueElement(Type vt, XmlElement e)
		{
			var gt = vt.GetGenericTypeDefinition();
			if (gt == typeof(KeyValuePair<,>))
				return ParseKeyValuePairElement(vt, e);
			else
				throw new NotSupportedException("Unsupported generic value type: " + vt.FullName);
		}

		static object ParseKeyValuePairElement(Type vt, XmlElement e)
		{
			var its = vt.GetGenericArguments();
			object key = ParseValue(its[0], e.GetAttribute("Key"));
			var attr = e.GetAttributeNode("Value");
			object value = null;
			if (attr != null)
				value = ParseValue(its[1], attr.Value);
			else
				throw new NotSupportedException("Value element of KeyValuePair is not implmeneted");
			return Activator.CreateInstance(vt, key, value);
		}

		public static object ParseObjectElement(Type vt, XmlElement e)
		{
			var o = Activator.CreateInstance(vt);
			foreach (XmlAttribute attr in e.Attributes)
			{
				if (attr.Name != "_key")
					LoadAttr(vt, o, attr);
			}

			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
					LoadChild(vt, o, (XmlElement)node);
			}

			// Try after load method
			var mi = vt.GetMethod("_SimpleXml_AfterLoad", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (mi != null)
				mi.Invoke(o, new object[0]);

			return o;
		}

		static object ParseArrayElement(Type vt, XmlElement e)
		{
			Debug.Assert(vt.BaseType == typeof(Array));
			var it = vt.GetElementType();
			var esubs = new List<XmlElement>();
			foreach (XmlNode node in e.ChildNodes)
				if (node.NodeType == XmlNodeType.Element)
					esubs.Add((XmlElement)node);

			var array = Array.CreateInstance(it, esubs.Count);
			for (int i = 0; i < array.Length; ++i)
				array.SetValue(ParseElement(it, esubs[i]), i);
			return array;
		}

		static object ParseGenericElement(Type vt, XmlElement e)
		{
			var gt = vt.GetGenericTypeDefinition();
			if (gt == typeof(List<>))
				return ParseListElement(vt, e);
			else if (gt == typeof(Dictionary<,>))
				return ParseDictionaryElement(vt, e);
			else
				throw new NotSupportedException("Unsupported generic type: " + vt.FullName);
		}

		static object ParseListElement(Type vt, XmlElement e)
		{
			var it = vt.GetGenericArguments()[0];
			var list = (System.Collections.IList)Activator.CreateInstance(vt);
			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					object item = ParseElement(it, (XmlElement)node);
					list.Add(item);
				}
			}
			return list;
		}

		static object ParseDictionaryElement(Type vt, XmlElement e)
		{
			var its = vt.GetGenericArguments();
			Debug.Assert(its.Length == 2);
			var dict = (System.Collections.IDictionary)Activator.CreateInstance(vt);
			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					var eItem = (XmlElement)node;
					object key = ParseValue(its[0], eItem.GetAttribute("_key"));
					object item = ParseElement(its[1], (XmlElement)node);
					dict.Add(key, item);
				}
			}
			return dict;
		}

		#endregion

		#region Attribute Values

		static object ParseValue(Type vt, string value)
		{
			if (vt == typeof(string))
				return value;
			else if (vt.IsValueType)
				return ParseValueTypeValue(vt, value);
			else
				throw new NotSupportedException("Unsupported type: " + vt.FullName);
		}

		static object ParseValueTypeValue(Type vt, string value)
		{
			if (vt.IsPrimitive)
			{
				// Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, Single.
				switch (vt.FullName)
				{
					case "System.Boolean":
						return bool.Parse(value);
					case "System.Byte":
						return byte.Parse(value);
					case "System.SByte":
						return sbyte.Parse(value);
					case "System.Int16":
						return short.Parse(value);
					case "System.UInt16":
						return ushort.Parse(value);
					case "System.Int32":
						return int.Parse(value);
					case "System.UInt32":
						return uint.Parse(value);
					case "System.Int64":
						return long.Parse(value);
					case "System.UInt64":
						return ulong.Parse(value);
					case "System.Char":
						return char.Parse(value);
					case "System.Double":
						return double.Parse(value);
					case "System.Single":
						return float.Parse(value);
					default:
						throw new NotSupportedException("Unsupported primitive value type: " + vt.FullName);
				}
			}
			else if (vt.IsEnum)
				return ParseEnumValue(vt, value);
			else
				throw new NotSupportedException("Unsupported value type: " + vt.FullName);
		}

		static object ParseEnumValue(Type vt, string value)
		{
			return Enum.Parse(vt, value);
		}

		#endregion

		#region Fields

		static FieldInfo GetField(Type vt, string name, out CustomNodeAttribute custom)
		{
			var field = vt.GetField(name);
			if (field == null)
				throw new NotSupportedException("Unsupported field name: " + name);

			var attrs = field.GetCustomAttributes(typeof(CustomNodeAttribute), true);
			if (attrs.Length != 0)
				custom = (CustomNodeAttribute)attrs[0];
			else
				custom = null;

			return field;
		}

		static object InvokeLoadAttrMethod(Type vt, object instance, CustomNodeAttribute custom, string value)
		{
			if (custom.Mode != CustomNodeMode.Attr)
				throw new NotSupportedException("Unsupported custom attribute node");

			var method = vt.GetMethod(custom.Loader, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			if (method == null)
				throw new NotSupportedException("Loader method not found: " + custom.Loader);

			return method.Invoke(instance, new object[] { value });
		}

		static object InvokeLoadElementMethod(Type vt, CustomNodeAttribute custom, XmlElement e)
		{
			if (custom.Mode != CustomNodeMode.Element)
				throw new NotSupportedException("Unsupported custom element node");

			var method = vt.GetMethod(custom.Loader);
			if (method == null)
				throw new NotSupportedException("Loader method not found: " + custom.Loader);
			if (!method.IsStatic)
				throw new NotSupportedException("Loader method should be static");

			return method.Invoke(null, new object[] { e });
		}

		static void LoadAttr(Type vt, object o, XmlAttribute attr)
		{
			CustomNodeAttribute custom;
			var field = GetField(vt, attr.Name, out custom);

			object ov = null;
			if (custom != null)
				ov = InvokeLoadAttrMethod(vt, o, custom, attr.Value);
			else
				ov = ParseValue(field.FieldType, attr.Value);

			field.SetValue(o, ov);
		}

		static void LoadChild(Type vt, object o, XmlElement e)
		{
			CustomNodeAttribute custom;
			var field = GetField(vt, e.Name, out custom);

			object ov = null;
			if (custom != null)
				ov = InvokeLoadElementMethod(vt, custom, e);
			else
				ov = ParseElement(field.FieldType, e);

			field.SetValue(o, ov);
		}

		#endregion
	}

	public class SimpleXmlSaver
	{
		XmlWriter m_xmlw = null;

		public void SaveXmlDocument<T>(XmlWriter xmlw, string name, T o)
		{
			m_xmlw = xmlw;
			m_xmlw.WriteStartDocument();
			WriteObject(o.GetType(), name, o);
			m_xmlw.WriteEndDocument();
			m_xmlw.Close();
		}

		public void SaveXmlDocument<T>(string path, string name, T o)
		{
			var xmlw = Nano.Xml.XmlKit.CreateXmlWriter(path);
			SaveXmlDocument(xmlw, name, o);
		}

		public void SaveXmlDocument<T>(Stream stream, string name, T o)
        {
			var xmlw = Xml.XmlKit.CreateXmlWriter(stream);
			SaveXmlDocument(xmlw, name, o);
		}

		public void SaveXmlDocument<T>(TextWriter tw, string name, T o)
        {
			var xmlw = Xml.XmlKit.CreateXmlWriter(tw);
			SaveXmlDocument(xmlw, name, o);
		}

		void WriteObject(Type vt, string name, object o, Type tKey = null, object vKey = null)
		{
			Debug.Assert(vt != null);
			m_xmlw.WriteStartElement(name);

			if (tKey != null)
				WriteValueAttr(tKey, "_key", vKey);

			if (o != null)
			{
				if (IsValueType(vt))
					WriteValueAttr(vt, "value", o);
				else if (vt.IsArray)
					WriteArrayElement(vt, o);
				else if (vt.IsGenericType)
					WriteGenericElement(vt, o);
				else if (vt.IsClass)
					WriteObjectElement(vt, o);
				else
					throw new NotSupportedException("Unsupported type: " + vt.FullName);
			}
			else
				m_xmlw.WriteAttributeString("_null", "1");	// null value

			m_xmlw.WriteEndElement();
		}

		void WriteGenericValueElement(Type vt, object o)
		{
			var gt = vt.GetGenericTypeDefinition();
			if (gt == typeof(KeyValuePair<,>))
				WriteKeyValuePairElement(vt, o);
			else
				throw new NotSupportedException("Unsupported generic value type: " + vt.FullName);
		}

		void WriteArrayElement(Type vt, object o)
		{
			Debug.Assert(vt.BaseType == typeof(Array));
			var it = vt.GetElementType();

			Array array = (Array)o;
			foreach (object item in array)
				WriteObject(it, "i", item);
		}

		void WriteGenericElement(Type vt, object o)
		{
			var gt = vt.GetGenericTypeDefinition();
			if (gt == typeof(List<>))
				WriteListElement(vt, o);
			else if (gt == typeof(Dictionary<,>))
				WriteDictionaryElement(vt, o);
			else if (gt == typeof(KeyValuePair<,>))
				WriteKeyValuePairElement(vt, o);
			else
				throw new NotSupportedException("Unsupported generic type: " + vt.FullName);
		}

		void WriteListElement(Type vt, object o)
		{
			var it = vt.GetGenericArguments()[0];
			var list = (System.Collections.IList)o;
			foreach (object item in list)
				WriteObject(it, "i", item);
		}

		void WriteDictionaryElement(Type vt, object o)
		{
			var its = vt.GetGenericArguments();
			Debug.Assert(its.Length == 2);
			var dict = (System.Collections.IDictionary)o;
			foreach (System.Collections.DictionaryEntry pair in dict)
				WriteObject(its[1], "i", pair.Value, its[0], pair.Key);
		}

		void WriteKeyValuePairElement(Type vt, object o)
		{
			var prop = vt.GetProperty("Key");
			var fo = prop.GetValue(o, null);
			WriteValueAttr(prop.PropertyType, "Key", fo);

			prop = vt.GetProperty("Value");
			var ft = prop.PropertyType;
			fo = prop.GetValue(o, null);
			if (ft.IsPrimitive || ft.IsEnum)
				WriteValueAttr(ft, "Value", fo);
			else
				WriteObject(ft, "Value", fo);
		}

		void WriteObjectElement(Type vt, object o)
		{
			var fields = vt.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			List<WriteField> values, elements;
			ClassifyFields(fields, out values, out elements);

			foreach (var wf in values)
			{
				var field = wf.Field;
				var fv = field.GetValue(o);
				if (wf.Attr != null)
				{
					var mi = vt.GetMethod(wf.Attr.Saver, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					if (mi == null)
						throw new NotSupportedException("Saver method not found: " + wf.Attr.Saver);
					fv = mi.Invoke(o, new object[] { fv });
					WriteValueAttr(field.FieldType, field.Name, fv);
				}
				else
					WriteValueAttr(field.FieldType, field.Name, fv);
			}

			foreach (var wf in elements)
			{
				var field = wf.Field;
				var fv = field.GetValue(o);
				if (wf.Attr != null)
				{
					var mi = vt.GetMethod(wf.Attr.Saver, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					if (mi == null)
						throw new NotSupportedException("Saver method not found: " + wf.Attr.Saver);
					mi.Invoke(o, new object[] { m_xmlw, fv });
				}
				else
					WriteObject(field.FieldType, field.Name, fv);
			}
		}

		class WriteField
		{
			public FieldInfo Field;
			public CustomNodeAttribute Attr;
		}

		static bool IsValueType(Type it)
		{
			return it.IsPrimitive || it.IsEnum || it == typeof(string);
		}

		static void ClassifyFields(FieldInfo[] fields, out List<WriteField> values, out List<WriteField> elements)
		{
			values = new List<WriteField>();
			elements = new List<WriteField>();
			foreach (var field in fields)
			{
				var it = field.FieldType;
				var attrs = field.GetCustomAttributes(typeof(CustomNodeAttribute), true);
				if (attrs.Length != 0)
				{
					var attr = (CustomNodeAttribute)attrs[0];
					var wf = new WriteField { Field = field, Attr = attr };
					if (attr.Mode == CustomNodeMode.Attr)
						values.Add(wf);
					else if (attr.Mode == CustomNodeMode.Element)
						elements.Add(wf);
					else if (attr.Mode != CustomNodeMode.Ignore)
						throw new ArgumentException("Wrong CustomNodeMode");
				}
				else
				{
					var wf = new WriteField { Field = field, Attr = null };
					if (IsValueType(it))
						values.Add(wf);
					else
						elements.Add(wf);
				}
			}
		}

		#region Write Value

		void WriteValueAttr(Type vt, string name, object o)
		{
			if (o != null)
				m_xmlw.WriteAttributeString(name, o.ToString());
		}

		#endregion
	}
}
