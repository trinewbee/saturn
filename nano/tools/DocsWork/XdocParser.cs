using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace DocsWork
{
	class AsmXdocParser
	{
		AsmxData m_data;
		AsmxAssembly m_asm;

		public AsmXdocParser(AsmxData data)
		{
			m_data = data;
			m_asm = null;
		}

		public void Load(string path)
		{
			var xmldoc = new XmlDocument();
			xmldoc.Load(path);
			var eDoc = xmldoc.DocumentElement;
			Debug.Assert(eDoc.Name == "doc");

			var eAsm = (XmlElement)eDoc.SelectSingleNode("assembly");
			LoadAsm(eAsm);

			var eMbs = (XmlElement)eDoc.SelectSingleNode("members");
			LoadMembers(eMbs);
		}

		void LoadAsm(XmlElement e)
		{
			Debug.Assert(m_asm == null);
			var eName = (XmlElement)e.SelectSingleNode("name");
			string name = GetTextValue(eName);
			m_asm = m_data.Asms[name];
			Debug.Assert(m_asm != null);
		}

		void LoadMembers(XmlElement e)
		{
			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType != XmlNodeType.Element)
					continue;
				var eMember = (XmlElement)node;
				Debug.Assert(eMember.Name == "member");
				LoadMember(eMember);
			}
		}

		void LoadMember(XmlElement e)
		{
			string name = e.GetAttribute("name");
			if (name.StartsWith("T:"))
				LoadType(e);
			else if (name.StartsWith("F:"))
				LoadField(e);
			else if (name.StartsWith("P:"))
				LoadProperty(e);
			else if (name.StartsWith("M:"))
				LoadMethod(e);
			else
				throw new ArgumentException("Unknown member name: " + name);
		}

		void LoadType(XmlElement e)
		{
			string name = e.GetAttribute("name").Substring(2);   // T:Nano.Common.BinaryValue
			AsmxType ti = m_data.GetTypeByRefd(name);
			if (ti == null)
			{
				Console.WriteLine("Class not found: " + name);
				return;
			}

			ti.Xde = e;
		}

		void LoadField(XmlElement e)
		{

		}

		void LoadProperty(XmlElement e)
		{

		}

		void LoadMethod(XmlElement e)
		{
			string name = e.GetAttribute("name").Substring(2);      // M:Nano.Common.ExtConvert.CopyToArrayBE16(System.Byte[],System.Int32,System.UInt16)
			string tyname, mname, sig;
			SplitMethodName(name, out tyname, out mname, out sig);

			AsmxType ti = m_data.GetTypeByRefd(tyname);
			if (ti == null)
			{
				Console.WriteLine("Class not found: " + tyname);
				return;
			}

			AsmxMethod mi = ti.GetMethodByRefd(mname, sig);
			if (mi == null)
			{
				Console.WriteLine("Method not found: " + tyname + '.' + mname + '(' + sig + ')');
				return;
			}

			mi.Xde = e;
		}

		void SplitMethodName(string name, out string tyname, out string mname, out string sig)
		{
			if (name.EndsWith(")"))
			{
				int posL = name.IndexOf('(');
				Debug.Assert(posL > 0);
				tyname = name.Substring(0, posL);
				sig = name.Substring(posL + 1, name.Length - posL - 2);
			}
			else
			{
				tyname = name;
				sig = "";
			}
			int pos = tyname.LastIndexOf('.');
			Debug.Assert(pos > 0);
			mname = tyname.Substring(pos + 1);
			tyname = tyname.Substring(0, pos);
		}

		static string GetTextValue(XmlElement e)
		{
			string value = "";
			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
					value += node.Value;
			}
			return value;
		}

		static string GetParentName(string name)
		{
			int pos = name.LastIndexOf('.');
			Debug.Assert(pos > 0);
			return name.Substring(0, pos);
		}
	}
}
