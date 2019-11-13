using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Collection;

namespace Nano.Ext.CodeModel.CSharp
{
    [Obsolete()]
	public class CsModelBuilder
	{
        public CsModel Model;

        public CsModelBuilder() { Model = new CsModel(); }

        public CsModelBuilder(CsModel model) { Model = model; }

		public CsType CreateClass(string fullname, CsTypeModifier modifiers, string baseClass, params string[] interfaces)
		{
			string name = fullname.Substring(fullname.LastIndexOf('.') + 1);
			CsType ti = new CsType(CsTypeFlags.Class, modifiers, fullname, name);
			ti.BaseType = baseClass;
			ti.Interfaces = interfaces;

			Model.Tis.Add(ti.FullName, ti);
			return ti;
		}

		public CsType CreateNestClass(string nestType, string name, CsTypeModifier modifier, string baseClass, params string[] interfaces)
		{
			CsType nestTi = Model.Tis[nestType];
			string fullname = nestTi.FullName + '.' + name;
			CsType ti = new CsType(CsTypeFlags.Class, modifier, fullname, name);
			ti.NestType = nestType;
			ti.BaseType = baseClass;
			ti.Interfaces = interfaces;
			nestTi.NestedTypes.Add(ti.FullName);

			Model.Tis.Add(ti.FullName, ti);
			return ti;
		}

		public CsType RegisterGenericType(CsType gt, params CsType[] vts)
		{
			var sbf = new StringBuilder(gt.FullName);
			var sbs = new StringBuilder(Model.GetDisplayName(gt));
			sbf.Append('<');
			sbs.Append('<');
			bool first = true;
			foreach (var vt in vts)
			{
				if (!first)
				{
					sbf.Append(", ");
					sbs.Append(", ");
				}
				sbf.Append(vt.FullName);
				sbs.Append(Model.GetDisplayName(vt));
				first = false;
			}
			sbf.Append('>');
			sbs.Append('>');

			string fullname = sbf.ToString();
			string name = sbs.ToString();
			if (Model.Tis.ContainsKey(fullname))
				return Model.Tis[fullname];

			CsType ti = new CsType(CsTypeFlags.Class | CsTypeFlags.GenericInstance, CsTypeModifier.None, fullname, name);
			ti.GenericType = gt.FullName;
			ti.GenericParams = CollectionKit.Transform(vts, x => x.FullName);
			Model.Tis.Add(ti.FullName, ti);
			return ti;
		}

		public CsMethod CreateMethod(CsType ti, string name, CsMemberModifier modifier, CsVar[] args, CsVar ret)
		{
			CsMethod mi = new CsMethod() { Name = name, Flags = CsMemberFlags.None, Modifier = modifier, Args = args, Ret = ret };
			ti.Methods.Add(mi);
			return mi;
		}

		public CsMethod CreateConstructor(CsType ti, CsMemberModifier modifier, CsVar[] args)
		{
			var mi = new CsMethod() { Name = ti.Name, Flags = CsMemberFlags.Constructor, Modifier = modifier, Args = args, Ret = null };
			ti.Methods.Add(mi);
			return mi;
		}
	}
}
