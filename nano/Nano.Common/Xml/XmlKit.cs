using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using Nano.Common;

namespace Nano.Xml
{
	/// <summary>XmlKit.LoadTree 的回调函数</summary>
	/// <typeparam name="T">对象类型</typeparam>
	/// <remarks>参见 XmlKit.LoadTree 方法。</remarks>
	public interface IXmlNodeAccept<T>
	{
		/// <summary>根据XML节点构造对象</summary>
		/// <param name="parent">父对象</param>
		/// <param name="e">XML元素</param>
		/// <returns>根据给定元素构造的对象</returns>
		T BuildFromNode(T parent, XmlElement e);
	}

	public interface IXmlNodeWriter<T>
	{
		void BeginWrite(XmlWriter xmlw, T item);
		void EndWrite(XmlWriter xmlw, T item);
	}

	public class XmlKit
	{
		#region Create writer

		static XmlWriterSettings m_xmlws;

		static XmlKit()
		{
			m_xmlws = new XmlWriterSettings();
			m_xmlws.NewLineChars = "\r\n";
			m_xmlws.Indent = true;
			m_xmlws.IndentChars = "  ";
		}

		public static XmlWriter CreateXmlWriter(Stream stream)
		{
			return XmlTextWriter.Create(stream, m_xmlws);
		}

		public static XmlWriter CreateXmlWriter(TextWriter tw)
		{
			return XmlTextWriter.Create(tw, m_xmlws);
		}

		public static XmlWriter CreateXmlWriter(string path)
		{
			return XmlTextWriter.Create(path, m_xmlws);
		}

		#endregion

		#region Load / save tree

		/// <summary>将给定的XML元素（子树）构造对象树</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="e">根XML元素</param>
		/// <param name="parent">父对象</param>
		/// <param name="accp">用于构建对象树的回调接口</param>
		/// <remarks>
		/// 该方法将递归遍历以给定XML元素为根的XML子树，并对遍历的每一个节点调用IXmlNodeAccept接口，最后形成对象树。
		/// </remarks>
		public static void LoadTree<T>(XmlElement e, T parent, IXmlNodeAccept<T> accp)
		{
			foreach (XmlNode nodeSub in e.ChildNodes)
			{
				if (nodeSub.NodeType == XmlNodeType.Element)
				{
					XmlElement eSub = (XmlElement)nodeSub;
					T itemSub = accp.BuildFromNode(parent, eSub);
					LoadTree<T>(eSub, itemSub, accp);
				}
			}
		}

		/// <summary>读取XML文件，并构造对象树</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="path">XML 文件名</param>
		/// <param name="root">根对象</param>
		/// <param name="accp">用于构建对象树的回调接口</param>
		/// <param name="openExist">当文件不存在时，openExist 为 true 将产生异常。</param>
		/// <remarks>参见 LoadTree&lt;T&gt;(XmlElement, T, IXmlNodeAccept&lt;T&gt;)方法</remarks>
		public static void LoadTree<T>(string path, T root, IXmlNodeAccept<T> accp, bool openExist)
		{
			if (!File.Exists(path))
			{
				if (openExist)
					throw new IOException("File not found");
				else
					return;
			}

			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			LoadTree<T>(doc.DocumentElement, root, accp);
		}

		public static void SaveTree<T>(XmlWriter xmlw, T item, ITreeVisitor<T> visitor, IXmlNodeWriter<T> wr)
		{
			wr.BeginWrite(xmlw, item);
			foreach (T subitem in visitor.GetChildren(item))
				SaveTree<T>(xmlw, subitem, visitor, wr);
			wr.EndWrite(xmlw, item);
		}

		public static void SaveTree<T>(string path, T root, ITreeVisitor<T> visitor, IXmlNodeWriter<T> wr)
		{
			XmlWriter xmlw = CreateXmlWriter(path);
			xmlw.WriteStartDocument();
			SaveTree<T>(xmlw, root, visitor, wr);
			xmlw.WriteEndDocument();
			xmlw.Close();
		}

		#endregion

		#region Read attributes

		public static string GetAttr(XmlElement e, string name, string defv)
		{
			var attr = e.GetAttributeNode(name);
			return attr != null ? attr.Value : defv;
		}

		public static string GetAttr(XmlElement e, string name) => e.GetAttribute(name);

		public static int GetAttrInt(XmlElement e, string name, int defv)
		{
			var attr = e.GetAttributeNode(name);
			return attr != null ? int.Parse(attr.Value) : defv;
		}

		public static int GetAttrInt(XmlElement e, string name) => int.Parse(e.GetAttribute(name));

		public static long GetAttrLong(XmlElement e, string name, long defv)
		{
			var attr = e.GetAttributeNode(name);
			return attr != null ? long.Parse(attr.Value) : defv;
		}

		public static long GetAttrLong(XmlElement e, string name) => long.Parse(e.GetAttribute(name));

		public static bool GetAttrBool(XmlElement e, string name, bool defv)
		{
			var attr = e.GetAttributeNode(name);
			return attr != null ? bool.Parse(attr.Value) : defv;
		}

		public static bool GetAttrBool(XmlElement e, string name) => bool.Parse(e.GetAttribute(name));

		#endregion
	}
}
