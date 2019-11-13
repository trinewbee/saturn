using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace Nano.Xml
{
	public class XmlConfigParser
	{
		#region Load attribute value

		public static int ToInt(string val, int defv)
		{
			if (val != null)
			{
				if (val.StartsWith("0x"))
					return Convert.ToInt32(val, 16);
				else
					return Convert.ToInt32(val);
			}
			else
				return defv;
		}

		public static long ToLong(string val, long defv)
		{
			if (val != null)
			{
				if (val.StartsWith("0x"))
					return Convert.ToInt64(val, 16);
				else
					return Convert.ToInt64(val);
			}
			else
				return defv;
		}

		public static double ToDouble(string val, double defv)
		{
			if (val != null)
				return Convert.ToDouble(val);
			else
				return defv;
		}

		public static bool ToBool(string val, bool defv)
		{
			if (val != null)
			{
				val = val.ToLowerInvariant();
				if (val == "true")
					return true;
				else if (val == "false")
					return false;
				else
					return Convert.ToInt32(val) != 0;
			}
			else
				return defv;
		}

		public static string LoadAttrStr(XmlElement e, string name, string defv)
		{
			XmlAttribute attr = e.Attributes[name];
			if (attr != null)
				return attr.Value;
			else
				return defv;
		}

		public static int LoadAttrInt(XmlElement e, string name, int defv)
		{
			string val = LoadAttrStr(e, name, null);
			return ToInt(val, defv);
		}

		public static long LoadAttrLong(XmlElement e, string name, long defv)
		{
			string val = LoadAttrStr(e, name, null);
			return ToLong(val, defv);
		}

		public static double LoadAttrDbl(XmlElement e, string name, double defv)
		{
			string val = LoadAttrStr(e, name, null);
			return ToDouble(val, defv);
		}

		public static bool LoadAttrBool(XmlElement e, string name, bool defv)
		{
			string val = LoadAttrStr(e, name, null);
			return ToBool(val, defv);
		}

		#endregion

		public static XmlElement SelectSingleton(XmlElement e, bool fMustExist)
		{
			XmlElement eSub = null;
			foreach (XmlNode nodeSub in e.ChildNodes)
			{
				if (nodeSub.NodeType == XmlNodeType.Element)
				{
					if (eSub == null)
						eSub = (XmlElement)nodeSub;
					else
						throw new ArgumentException("More than one results found");
				}
			}
			if (fMustExist && eSub == null)
				throw new KeyNotFoundException();
			return eSub;
		}
	}
}
