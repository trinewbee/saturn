using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Nano.Common;
using Nano.Collection;

namespace SyncCode
{
	class SyncCodeApp : IFileUpdater, IFileUpdateNotify
	{
		string m_pathS, m_pathT;
		string m_prefix;
		List<string> m_items;

		SyncCodeApp(string pathS, string pathT)
		{
			m_pathS = pathS;
			m_pathT = pathT;
			m_prefix = null;
			m_items = new List<string>();
		}

		static HashSet<string> m_interns = new HashSet<string> { "obj", "bin", "Properties", "Resources" };

		void Sync()
		{
			var pathSis = Directory.GetDirectories(m_pathS);
			foreach (var pathSi in pathSis)
			{
				string name = Path.GetFileName(pathSi);
				if (name[0] == '.' || m_interns.Contains(name))
					continue;

				string pathTi = Path.Combine(m_pathT, name);
				m_prefix = name;
				var update = new FileUpdateWalker(pathSi, pathTi, this, FileUpdateWalkerOption.PerformDelete, this);
				update.Walk();
				m_prefix = null;
			}
		}

		static XmlElement SelectElement(XmlElement e, string name, Predicate<XmlElement> pred)
		{
			foreach (XmlNode node in e.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element && node.Name == name)
				{
					XmlElement esub = (XmlElement)node;
					if (pred(esub))
						return esub;
				}
			}
			return null;
		}

		static XmlElement SelectSourceItemGroup(XmlDocument doc)
		{
			XmlElement eIG = SelectElement(doc.DocumentElement, "ItemGroup", delegate (XmlElement e) {
				foreach (XmlNode node in e.ChildNodes)
				{
					XmlElement esub = node as XmlElement;
					if (esub != null && esub.Name == "Compile" && esub.GetAttribute("Include") == "Properties\\AssemblyInfo.cs")
						return true;
				}
				return false;
			});
			return eIG;
		}

		void MakeProject(string name)
		{
			string path = Path.Combine(m_pathT, name);
			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			var eIG = SelectSourceItemGroup(doc);
			Debug.Assert(eIG != null);

			List<XmlElement> list = new List<XmlElement>();
			foreach (XmlNode node in eIG.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element && node.Name == "Compile")
					list.Add((XmlElement)node);
			}
			
			foreach (XmlElement eI in list)
			{
				string pathCode = eI.GetAttribute("Include");
				int pos = pathCode.IndexOf('\\');
				string prefix = pathCode.Substring(0, pos >= 0 ? pos : pathCode.Length);
				if (!m_interns.Contains(prefix))
					eIG.RemoveChild(eI);
			}

			foreach (string s in m_items)
			{
				XmlElement eI = doc.CreateElement("Compile", "http://schemas.microsoft.com/developer/msbuild/2003");
				eI.SetAttribute("Include", s);
				eIG.AppendChild(eI);
			}

			doc.Save(path);
		}

		void IFileUpdater.Process(string source, string target)
		{
			File.Copy(source, target, true);
		}

		void IFileUpdateNotify.Send(string relative, FileUpdateActionType act)
		{
			switch (act)
			{
				case FileUpdateActionType.SkipFile:
				case FileUpdateActionType.UpdateFile:
					m_items.Add(m_prefix + relative);
					break;
			}
		}

		static void Main(string[] args)
		{
			Debug.Assert(args.Length == 2);
			string pathS = args[0];
			string pathT = args[1];
			string name = Path.GetFileName(pathT);

			var o = new SyncCodeApp(pathS, pathT);
			o.Sync();
			o.MakeProject(name + ".csproj");
		}
	}
}
