using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using Nano.Common;

namespace DocsWork
{
	class AsmxDump
	{
		AsmxData m_data;
		XmlWriter m_wr;

		public AsmxDump(AsmxData data)
		{
			m_data = data;
			m_wr = null;
		}

		public void Dump(string path)
		{
			m_wr = Nano.Xml.XmlKit.CreateXmlWriter(path);
			m_wr.WriteStartDocument();
			m_wr.WriteStartElement("asmx");

			foreach (var asm in m_data.Asms.Values)
				DumpAsm(asm);

			m_wr.WriteEndElement();
			m_wr.WriteEndDocument();
			m_wr.Close();
		}

		void DumpAsm(AsmxAssembly asm)
		{
			m_wr.WriteStartElement("assembly");
			m_wr.WriteAttributeString("name", asm.Name);

			foreach (var ns in asm.Nss.Values)
			{
				m_wr.WriteStartElement("namespace");
				m_wr.WriteAttributeString("name", ns.Name);

				foreach (var ti in ns.Tis.Values)
				{
					switch (ti.TClass)
					{
						case AsmxTypeClass.Class:
							DumpClassType(ti);
							break;
						case AsmxTypeClass.Interface:
							DumpInterfaceType(ti);
							break;
						case AsmxTypeClass.Struct:
							DumpStructType(ti);
							break;
						case AsmxTypeClass.Enum:
							DumpEnumType(ti);
							break;
						case AsmxTypeClass.Delegate:
							DumpDelegateType(ti);
							break;
					}
				}

				m_wr.WriteEndElement();
			}

			m_wr.WriteEndElement();
		}

		void DumpClassType(AsmxType ti)
		{
			m_wr.WriteStartElement("class");
			m_wr.WriteAttributeString("name", ti.Display);
			m_wr.WriteAttributeString("visible", ti.IsVisible.ToString());
			m_wr.WriteAttributeString("full", ti.Name);
			m_wr.WriteAttributeString("modifier", ClassDocGenerator.GetClassModifier(ti));

			DumpXde(ti.Xde);

			m_wr.WriteStartElement("fields");
			foreach (var field in ti.Fields)
				DumpField(field);
			m_wr.WriteEndElement();

			m_wr.WriteStartElement("props");
			foreach (var prop in ti.Props)
				DumpProp(prop);
			m_wr.WriteEndElement();

			m_wr.WriteStartElement("method");
			foreach (var method in ti.Methods)
				DumpMethod(method);
			m_wr.WriteEndElement();

			m_wr.WriteEndElement();
		}

		void DumpInterfaceType(AsmxType ti)
		{
			m_wr.WriteStartElement("interface");
			m_wr.WriteAttributeString("name", ti.Display);
			m_wr.WriteAttributeString("visible", ti.IsVisible.ToString());
			m_wr.WriteAttributeString("full", ti.Name);

			DumpXde(ti.Xde);

			m_wr.WriteEndElement();
		}

		void DumpStructType(AsmxType ti)
		{
			m_wr.WriteStartElement("struct");
			m_wr.WriteAttributeString("name", ti.Display);
			m_wr.WriteAttributeString("visible", ti.IsVisible.ToString());
			m_wr.WriteAttributeString("full", ti.Name);

			DumpXde(ti.Xde);

			m_wr.WriteEndElement();
		}

		void DumpEnumType(AsmxType ti)
		{
			m_wr.WriteStartElement("enum");
			m_wr.WriteAttributeString("name", ti.Display);
			m_wr.WriteAttributeString("visible", ti.IsVisible.ToString());
			m_wr.WriteAttributeString("full", ti.Name);

			DumpXde(ti.Xde);

			m_wr.WriteEndElement();
		}

		void DumpDelegateType(AsmxType ti)
		{
			m_wr.WriteStartElement("delegate");
			m_wr.WriteAttributeString("name", ti.Display);
			m_wr.WriteAttributeString("visible", ti.IsVisible.ToString());
			m_wr.WriteAttributeString("full", ti.Name);

			DumpXde(ti.Xde);

			m_wr.WriteEndElement();
		}

		void DumpField(AsmxField field)
		{
			m_wr.WriteStartElement("field");
			m_wr.WriteAttributeString("name", field.Name);
			m_wr.WriteAttributeString("type", field.ValueType);

			DumpXde(field.Xde);

			m_wr.WriteEndElement();
		}

		void DumpProp(AsmxProperty prop)
		{
			m_wr.WriteStartElement("prop");
			m_wr.WriteAttributeString("name", prop.Name);
			m_wr.WriteAttributeString("type", prop.ValueType);
			m_wr.WriteAttributeString("can-read", prop.CanRead.ToString());
			m_wr.WriteAttributeString("can-write", prop.CanWrite.ToString());

			DumpXde(prop.Xde);

			m_wr.WriteEndElement();
		}

		void DumpMethod(AsmxMethod method)
		{
			m_wr.WriteStartElement("method");
			m_wr.WriteAttributeString("name", method.Name);
			m_wr.WriteAttributeString("return", method.ReturnType);
			m_wr.WriteAttributeString("sig", method.RefdSig);
			DumpParameters(method.Parameters);

			DumpXde(method.Xde);

			m_wr.WriteEndElement();
		}

		void DumpParameters(IEnumerable<AsmxParameter> prms)
		{
			m_wr.WriteStartElement("params");
			foreach (var prm in prms)
			{
				m_wr.WriteStartElement("param");
				m_wr.WriteAttributeString("name", prm.Name);
				m_wr.WriteAttributeString("type", prm.ValueType);
				m_wr.WriteEndElement();
			}
			m_wr.WriteEndElement();
		}

		void DumpXde(XmlElement e)
		{
			if (e != null)
			{
				m_wr.WriteStartElement("comment");
				WriteNodes(m_wr, e.ChildNodes);
				m_wr.WriteEndElement();
			}
		}

		#region Xml Clone

		static void WriteNode(XmlWriter wr, XmlNode node)
		{
			switch (node.NodeType)
			{
				case XmlNodeType.Element:
					WriteElement(wr, (XmlElement)node);
					break;
				case XmlNodeType.Text:
					wr.WriteValue(node.Value);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		static void WriteElement(XmlWriter wr, XmlElement e)
		{
			wr.WriteStartElement(e.Name);
			foreach (XmlAttribute attr in e.Attributes)
				wr.WriteAttributeString(attr.Name, attr.Value);
			WriteNodes(wr, e.ChildNodes);
			wr.WriteEndElement();
		}

		static void WriteNodes(XmlWriter wr, XmlNodeList nodes)
		{
			foreach (XmlNode node in nodes)
				WriteNode(wr, node);
		}

		#endregion
	}

	class DocsWorkApp
	{
		AsmxData m_data = new AsmxData();

		void LoadAssembly(string path)
		{
			AsmMetaParser amp = new AsmMetaParser();
			amp.Load(path);
			var asmx = amp.Asmx;
			m_data.Asms.Add(asmx.Name, asmx);
		}

		void FinalAssemblies()
		{
			m_data.MakeRefdTypeMap();
		}

		void LoadAsmXml(string path)
		{
			var axc = new AsmXdocParser(m_data);
			axc.Load(path);
		}

		void Dump(string path)
		{
			var dump = new AsmxDump(m_data);
			dump.Dump(path);
		}

		void GenHtmlDocs(string pathOut, string pathCss)
		{
			if (!Directory.Exists(pathOut))
				Directory.CreateDirectory(pathOut);

			var g = new HtmlGenerator(m_data, pathOut, pathCss);
			g.Generate();
		}

		static void Main(string[] args)
		{
			string pathNano = @"..\..\..\..";
			DocsWorkApp app = new DocsWorkApp();

			var names = new string[] { "Nano.Common", "Nano.Extensive", "Nano.Xapi" };
			foreach (var name in names)
				app.LoadAssembly(Path.Combine(pathNano, @"out\debug", name + ".dll"));

			app.FinalAssemblies();

			names = new string[] { "Nano.Common", "Nano.Extensive", "Nano.Xapi" };
			foreach (var name in names)
				app.LoadAsmXml(Path.Combine(pathNano, @"out\gen\xdoc", name + ".xml"));

			// app.Dump(Path.Combine(pathOutput, @"asmx.xml"));

			app.GenHtmlDocs(Path.Combine(pathNano, @"doc\xref"), "../main.css");
		}
	}
}
