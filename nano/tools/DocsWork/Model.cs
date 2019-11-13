using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Nano.Collection;

namespace DocsWork
{
	abstract class AsmxMember
	{
		public string Name = null;
		public AsmxType Ti = null;
		public XmlElement Xde = null;
	}

	class AsmxField : AsmxMember
	{
		public string ValueType = null;
	}

	class AsmxProperty : AsmxMember
	{
		public string ValueType = null;
		public bool CanRead = false, CanWrite = false;
	}

	public class AsmxParameter
	{
		public enum Flag
		{
			RefArg = 1,
			OutArg = 2,
			Optional = 4
		}

		public string Name;
		public string ValueType;
		public Flag Flags;
		public object Defv;
	}

	class AsmxMethod : AsmxMember
	{
		public string ReturnType = null;
		public AsmxParameter[] Parameters = null;
		public string RefdName = null;
		public string RefdSig = null;
	}

	enum AsmxTypeClass
	{
		Unknown, Class, Interface, Enum, Struct, Delegate
	}

	class AsmxType
	{
		public string Name = null;  // full name with namespace
		public AsmxNamespace NS = null;    // namespace
		public string Display = null;    // display name
		public string Refd = null;      // refered name in xml comments

		// Reflection fields
		public bool IsVisible = false;
		public AsmxTypeClass TClass = AsmxTypeClass.Unknown;
		public TypeAttributes Attributes = 0;
		public string NestOwner = null;
		public string BaseType = null;
		public string[] Interfaces = null;

		// XML Comment Document
		public XmlElement Xde = null;

		public List<AsmxField> Fields = new List<AsmxField>();
		public List<AsmxProperty> Props = new List<AsmxProperty>();
		public List<AsmxMethod> Methods = new List<AsmxMethod>();

		public AsmxMethod GetMethodByRefd(string name, string sig)
		{
			var mis = CollectionKit.Select(Methods, x => x.RefdName == name && x.RefdSig == sig);
			if (mis.Count == 1)
				return mis[0];
			else if (mis.Count == 0)
				return null;
			else
				throw new NotSupportedException();
		}

		public void SortMembers()
		{
			Fields.Sort(MemberComparer);
			Props.Sort(MemberComparer);
			Methods.Sort(MemberComparer);
		}

		static int MemberComparer(AsmxMember lhs, AsmxMember rhs)
		{
			return string.Compare(lhs.Name, rhs.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	class AsmxNamespace
	{
		public string Name = null;
		public SortedList<string, AsmxType> Tis = new SortedList<string, AsmxType>();
	}

	class AsmxAssembly
	{
		public string Name = null;
		public SortedList<string, AsmxNamespace> Nss = new SortedList<string, AsmxNamespace>();

		public AsmxNamespace GetNamespace(string name)
		{
			AsmxNamespace ns;
			if (Nss.TryGetValue(name, out ns))
				return ns;
			return null;
		}

		public AsmxNamespace ValidateNamespace(string name)
		{
			AsmxNamespace ns;
			if (Nss.TryGetValue(name, out ns))
				return ns;

			ns = new AsmxNamespace() { Name = name };
			Nss.Add(name, ns);
			return ns;
		}
	}

	class AsmxData
	{
		public SortedList<string, AsmxAssembly> Asms = new SortedList<string, AsmxAssembly>();
		public Dictionary<string, AsmxType> TypeMap = null;

		public void MakeRefdTypeMap()
		{
			TypeMap = new Dictionary<string, AsmxType>();
			foreach (var asm in Asms.Values)
			{
				foreach (var ns in asm.Nss.Values)
				{
					foreach (var ti in ns.Tis.Values)
					{
						string name = ti.Refd;
						TypeMap.Add(name, ti);
					}
				}
			}
		}

		public AsmxType GetTypeByRefd(string name)
		{
			AsmxType ti;
			if (TypeMap.TryGetValue(name, out ti))
				return ti;
			return null;
		}
	}
}
