using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;

namespace DocsWork
{
	public class HtmlWriter
	{
		XmlWriter m_wr;

		public HtmlWriter(XmlWriter wr)
		{
			m_wr = wr;
			m_wr.WriteStartDocument();
			m_wr.WriteStartElement("html");
		}

		public void WriteHeader(string title, string css)
		{
			m_wr.WriteStartElement("head");

			// <meta content="zh-cn" http-equiv="Content-Language" />
			WritePara("meta", null, new Dictionary<string, string> { { "content", "zh-cn" }, { "http-equiv", "Content-Language" } });
			// <meta content="text/html; charset=utf-8" http-equiv="Content-Type" />
			WritePara("meta", null, new Dictionary<string, string> { { "content", "text/html; charset=utf-8" }, { "http-equiv", "Content-Type" } });

			WritePara("title", title);

			// <link href="main.css" rel="stylesheet" type="text/css" />
			WritePara("link", null, new Dictionary<string, string> { { "href", css }, { "rel", "stylesheet" }, { "type", "text/css" } });

			m_wr.WriteEndElement();	// head
		}

		public void BeginTag(string name, Dictionary<string, string> args = null)
		{
			m_wr.WriteStartElement(name);
			if (args != null)
			{
				foreach (var arg in args)
					m_wr.WriteAttributeString(arg.Key, arg.Value);
			}
		}

		public void WriteSingle(string tag, Dictionary<string, string> args = null)
		{
			BeginTag(tag, args);
			EndTag(tag);
		}

		public void WriteText(string s)
		{
			m_wr.WriteValue(s);
		}

		public void WriteText(object o)
		{
			m_wr.WriteValue(o.ToString());
		}

		public void EndTag(string name)
		{
			m_wr.WriteEndElement();
		}

		public void WritePara(string tag, string text, Dictionary<string, string> args = null)
		{
			BeginTag(tag, args);
			if (text != null)
				m_wr.WriteValue(text);
			EndTag(tag);
		}

		public void Close()
		{
			m_wr.WriteEndElement(); // html
			m_wr.WriteEndDocument();
			m_wr.Close();
		}
	}

	class ClassDocGenerator
	{
		AsmxType m_ti;
		HtmlWriter m_wr;
		string m_pathCss;

		public ClassDocGenerator(AsmxType ti, string pathOut, string pathCss)
		{
			m_ti = ti;
			var xmlw = Nano.Xml.XmlKit.CreateXmlWriter(pathOut);
			m_wr = new HtmlWriter(xmlw);
			m_pathCss = pathCss;
		}

		public void Generate()
		{
			m_wr.WriteHeader(GetTitle(), m_pathCss);
			m_wr.BeginTag("body");

			WriteClassAttr();
			WriteMemberIndex();
			WriteMethods();
			WriteClassFooter();

			m_wr.EndTag("body");
			m_wr.Close();
		}

		void WriteClassAttr()
		{
			m_wr.WritePara("h1", GetTitle());
			WriteXde(m_ti.Xde);

			// Basic attributes
			m_wr.WritePara("h2", "基本信息");
			m_wr.BeginTag("p");

			var sb = new StringBuilder("定义：");
			sb.Append(GetClassModifier(m_ti));
			sb.Append("class ");
			sb.Append(m_ti.Display).Append(';');
			m_wr.WriteText(sb);

			if (m_ti.BaseType != null)
			{
				m_wr.WriteSingle("br");
				m_wr.WriteText("基类：" + m_ti.BaseType);
			}

			if (m_ti.Interfaces != null && m_ti.Interfaces.Length != 0)
			{
				sb.Clear();
				sb.Append("实现接口：");
				foreach (var name in m_ti.Interfaces)
					sb.Append(name).Append(", ");
				sb.Remove(sb.Length - 2, 2);
				m_wr.WriteSingle("br");
				m_wr.WriteText(sb);
			}

			m_wr.EndTag("p");
		}

		public static string GetClassModifier(AsmxType ti)
		{
			string modifier;
			var attrSt = ti.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed);
			switch (attrSt)
			{
				case TypeAttributes.Abstract | TypeAttributes.Sealed:
					modifier = "static ";
					break;
				case TypeAttributes.Abstract:
					modifier = "abstract ";
					break;
				case TypeAttributes.Sealed:
					modifier = "sealed ";
					break;
				default:
					modifier = "";
					break;
			}
			return modifier;
		}

		string GetTitle()
		{
			return m_ti.NS.Name + '.' + m_ti.Display + " 类";
		}

		void WriteMemberIndex()
		{
			if (m_ti.Methods.Count != 0)
			{
				m_wr.WritePara("p", "方法列表");
				m_wr.BeginTag("table", new Dictionary<string, string> { { "class", "tborder" }, { "style", "width: 100%" } });

				m_wr.BeginTag("colgroup");
				m_wr.WritePara("col", "");
				m_wr.WritePara("col", "");
				m_wr.WritePara("col", "");
				m_wr.EndTag("colgroup");

				var tdArgs = new Dictionary<string, string> { { "class", "chead-middle" } };
				m_wr.BeginTag("tr");
				m_wr.WritePara("td", "名称", tdArgs);
				m_wr.WritePara("td", "定义", tdArgs);
				m_wr.WritePara("td", "简介", tdArgs);
				m_wr.EndTag("tr");

				tdArgs = new Dictionary<string, string> { { "class", "ctext" } };
				foreach (var mi in m_ti.Methods)
				{
					m_wr.BeginTag("tr");
					m_wr.WritePara("td", mi.Name, tdArgs);

					m_wr.BeginTag("td", tdArgs);
					string href = "#" + GetMethodAnchor(mi);
					var args = new Dictionary<string, string> { { "href", href } };
					m_wr.WritePara("a", GetMethodDisplay(mi), args);
					m_wr.EndTag("td");

					m_wr.BeginTag("td", tdArgs);
					if (mi.Xde != null)
					{
						var e = (XmlElement)mi.Xde.SelectSingleNode("summary");
						if (e != null)
							WriteXdeNode(e, null);
					}
					m_wr.EndTag("td");

					m_wr.EndTag("tr");
				}

				m_wr.EndTag("table");
			}
		}

		void WriteMethods()
		{
			m_wr.WritePara("h2", "方法列表");
			foreach (var mi in m_ti.Methods)
				WriteMethod(mi);
		}

		void WriteMethod(AsmxMethod mi)
		{
			string display = GetMethodDisplay(mi);
			string anchor = GetMethodAnchor(mi);
			var args = new Dictionary<string, string> { { "id", anchor } };
			m_wr.WritePara("h3", display, args);
			WriteXde(mi.Xde);
		}

		static string GetMethodAnchor(AsmxMethod mi)
		{
			return "M-" + mi.RefdName + '-' + mi.RefdSig;
		}

		string GetMethodDisplay(AsmxMethod mi)
		{
			var sb = new StringBuilder();

			sb.Append(mi.ReturnType).Append(' ');
			sb.Append(mi.Name).Append('(');

			var prms = mi.Parameters;
			bool first = true;
			foreach (var prm in prms)
			{
				if (!first)
					sb.Append(", ");
				first = false;
				sb.Append(GetPrmPrefix(prm));
				sb.Append(prm.ValueType).Append(' ').Append(prm.Name);
				if ((prm.Flags & AsmxParameter.Flag.Optional) != 0)
					sb.Append(" = ").Append(FormatDefv(prm.Defv));
			}
			sb.Append(')');

			return sb.ToString();
		}

		static string GetPrmPrefix(AsmxParameter prm)
		{
			string prefix = "";
			switch (prm.Flags & (AsmxParameter.Flag.RefArg | AsmxParameter.Flag.OutArg))
			{
				case AsmxParameter.Flag.RefArg:
					prefix = "ref ";
					break;
				case AsmxParameter.Flag.OutArg:
					prefix = "out ";
					break;
			}
			return prefix;
		}

		static string FormatDefv(object o)
		{
			if (o == null)
				return "null";

			switch (o.GetType().FullName)
			{
				case "System.Boolean":
					return (bool)o ? "true" : "false";
				case "System.String":
					return '\"' + (string)o + '\"';	// todo escape
				default:
					return o.ToString();
			}
		}

		void WriteClassFooter()
		{
			m_wr.WriteSingle("hr");
			m_wr.BeginTag("p");
			var args = new Dictionary<string, string> { { "href", "index.html" } };
			m_wr.WritePara("a", "index", args);
			m_wr.EndTag("p");
		}

		void WriteXde(XmlElement xde)
		{
			if (xde == null)
				return;

			var e = (XmlElement)xde.SelectSingleNode("summary");
			if (e != null)
				WriteXdeNode(e, "p");

			// <typeparam name="T">对象类型</typeparam>
			XmlNodeList nodes = xde.SelectNodes("typeparam");
			if (nodes.Count != 0)
			{
				m_wr.WritePara("p", "模板参数");
				m_wr.BeginTag("ul");
				foreach (XmlNode node in nodes)
				{
					e = (XmlElement)node;
					m_wr.BeginTag("li");
					m_wr.WriteText(e.GetAttribute("name") + ": ");
					WriteXdeNode(e, null);
					m_wr.EndTag("li");
				}
				m_wr.EndTag("ul");
			}

			// <param name="s">源对象集合的枚举器</param>
			nodes = xde.SelectNodes("param");
			if (nodes.Count != 0)
			{
				m_wr.WritePara("p", "输入参数");
				m_wr.BeginTag("ul");
				foreach (XmlNode node in nodes)
				{
					e = (XmlElement)node;
					m_wr.BeginTag("li");
					m_wr.WriteText(e.GetAttribute("name") + ": ");
					WriteXdeNode(e, null);
					m_wr.EndTag("li");
				}
				m_wr.EndTag("ul");
			}

			// <returns>返回构造的数组</returns>
			e = (XmlElement)xde.SelectSingleNode("returns");
			if (e != null)
			{
				m_wr.BeginTag("p");
				m_wr.WriteText("返回：");
				WriteXdeNode(e, null);
				m_wr.EndTag("p");
			}

			e = (XmlElement)xde.SelectSingleNode("remarks");
			if (e != null)
			{
				m_wr.BeginTag("p");
				m_wr.WritePara("b", "Remarks:");
				m_wr.EndTag("p");
				WriteXdeNode(e, "p");
			}
		}

		void WriteXdeNode(XmlElement e, string tag = null)
		{
			if (tag != null)
				m_wr.BeginTag(tag);
			foreach (XmlNode node in e.ChildNodes)
			{
				Debug.Assert(node.NodeType == XmlNodeType.Text);
				m_wr.WriteText(node.Value);
			}
			if (tag != null)
				m_wr.EndTag(tag);
		}
	}

	class HtmlGenerator
	{
		string m_pathOut;
		string m_pathCss;
		AsmxData m_data;

		public HtmlGenerator(AsmxData data, string pathOut, string pathCss)
		{
			m_pathOut = pathOut;
			m_pathCss = pathCss;
			m_data = data;
		}

		public void Generate()
		{
			GenerateMain();
			GenerateTypes();
		}

		void GenerateMain()
		{
			string path = Path.Combine(m_pathOut, "index.html");
			var xmlw = Nano.Xml.XmlKit.CreateXmlWriter(path);
			var wr = new HtmlWriter(xmlw);

			wr.WriteHeader("Library Documents", m_pathCss);
			wr.BeginTag("body");
			wr.WritePara("h1", "Library Documents");

			foreach (var asm in m_data.Asms.Values)
			{
				wr.WritePara("h2", "Assembly " + asm.Name);
				foreach (var ns in asm.Nss.Values)
				{
					wr.WritePara("h3", "Namespace " + ns.Name);
					wr.BeginTag("ul");
					foreach (var ti in ns.Tis.Values)
					{
						if (!ti.IsVisible)
							continue;

						string hyper = "T-" + ti.Refd + ".html";
						wr.BeginTag("li");
						var args = new Dictionary<string, string> { { "href", hyper } };
						wr.BeginTag("a", args);
						wr.WriteText(GetTypeHyperName(ti));
						wr.EndTag("a");
						wr.EndTag("li");
					}
					wr.EndTag("ul");
				}
			}

			wr.EndTag("body");

			wr.Close();
		}

		static string GetTypeHyperName(AsmxType ti)
		{
			return ti.TClass + " " + ti.Display;
		}

		void GenerateTypes()
		{
			foreach (var asm in m_data.Asms.Values)
			{
				foreach (var ns in asm.Nss.Values)
				{
					foreach (var ti in ns.Tis.Values)
					{
						if (!ti.IsVisible)
							continue;

						GenClassDoc(ti);
					}
				}
			}
		}

		void GenClassDoc(AsmxType ti)
		{
			string path = Path.Combine(m_pathOut, "T-" + ti.Refd + ".html");
			var g = new ClassDocGenerator(ti, path, m_pathCss);
			g.Generate();
		}
	}
}
