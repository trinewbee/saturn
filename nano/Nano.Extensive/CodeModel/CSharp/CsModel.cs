using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Ext.CodeModel.CSharp
{
    [Obsolete()]
    public enum CsTypeFlags
	{
		Mask_CT = 0x7,
		Value = 1,
		Struct = 2,
		Class = 3,
		Delegate = 4,

		External = 0x10,
		GenericInstance = 0x40,
	}

    [Obsolete()]
    public enum CsTypeModifier
	{
		None = 0,

		Mask_Protect = 0xF,
		Public = 1,
		Protected = 2,
		Private = 3,
		Internal = 4,

		Mask_Instance = 0xF0,
		Abstract = 0x10,
		Final = 0x30,   // sealed
		Static = 0x40
	}

    [Obsolete()]
    public class CsType
	{
		public CsTypeFlags Flags;
		public CsTypeModifier Modifiers;
		public string FullName;
		public string Name;
		public string NestType = null; // used in a nested type
		public string GenericType = null;   // used in a generic type instance
		public string BaseType = null;
		public string[] Interfaces = null;

		public List<string> NestedTypes = new List<string>();
		public List<string> GenericParams = new List<string>();
		public List<CsVar> Fields = new List<CsVar>();
		public List<CsMethod> Methods = new List<CsMethod>();

		public CsType(CsTypeFlags flags, CsTypeModifier modifier, string fullname, string name)
		{
			Flags = flags;
			Modifiers = modifier;
			FullName = fullname;
			Name = name;
		}
	}

    [Obsolete()]
    public enum CsMemberFlags
	{
		None = 0,
		Constructor = 1,
	}

    [Obsolete()]
    public enum CsMemberModifier
	{
		None = 0,

		Mask_Protect = 0xF,
		Public = 1,
		Protected = 2,
		Private = 3,
		Internal = 4,

		Mask_Instance = 0xF0,
		Abstract = 0x10,
		Virtual = 0x20,
		Final = 0x30,   // sealed (with override)
		Static = 0x40,

		Override = 0x100
	}

    [Obsolete()]
    public class CsMethod
	{
		public string Name;
		public CsMemberFlags Flags;
		public CsMemberModifier Modifier;
		public CsVar[] Args;
		public CsVar Ret;
		public List<CsStatement> Statements = new List<CsStatement>();
	}

    [Obsolete()]
    public enum CsStatementType
	{
		Expression,
		VarDecl,
		Return,
		If,
	}

    [Obsolete()]
    public class CsStatement
	{
		public class Body
		{
			public List<CsStatement> Statements = new List<CsStatement>();
		}

		public CsStatementType Type;
		public CsExpr Expr = null;              // Expression
		public CsVar Var = null;                // VarDecl
		public List<CsExpr> BrConds = null;     // If (if, else if)
		public List<Body> BrBodies = null;      // If (if, else if)
		public Body Else = null;                // If (else)

		CsStatement(CsStatementType type) { Type = type; }

		public Body AddBranch(CsExpr expr)
		{
			Debug.Assert(Type == CsStatementType.If);
			var body = new Body();
			BrConds.Add(expr);
			BrBodies.Add(body);
			return body;
		}

		public Body AddBranch(string expr) => AddBranch(new CsExpr(expr));

		public Body AddElse()
		{
			Debug.Assert(Type == CsStatementType.If);
			Debug.Assert(Else == null);
			return Else = new Body();
		}

		public static CsStatement MakeExpr(CsExpr expr)
		{
			return new CsStatement(CsStatementType.Expression) { Expr = expr };
		}

		public static CsStatement MakeExpr(string expr) => MakeExpr(new CsExpr(expr));

		public static CsStatement MakeVarDecl(CsVar v)
		{
			var st = new CsStatement(CsStatementType.VarDecl) { Var = v };
			return st;
		}

		public static CsStatement MakeVarDecl(string name, string type, CsExpr expr)
		{
			var v = new CsVar(name, type, CsMemberModifier.None, expr);
			return MakeVarDecl(v);
		}

		public static CsStatement MakeVarDecl(string name, string type, string expr) => MakeVarDecl(name, type, new CsExpr(expr));

		// return, break, continue
		public static CsStatement MakeSingle(CsStatementType type, CsExpr expr = null)
		{
			return new CsStatement(type) { Expr = expr };
		}

		public static CsStatement MakeSingle(CsStatementType type, string expr) => MakeSingle(type, new CsExpr(expr));

		public static CsStatement MakeIf()
		{
			var stmt = new CsStatement(CsStatementType.If);
			stmt.BrConds = new List<CsExpr>();
			stmt.BrBodies = new List<Body>();
			return stmt;
		}
	}

    [Obsolete()]
    public class CsVar
	{
		public string Name;
		public string Type;
		public CsMemberModifier Modifier;
		public CsExpr Expr;

		public CsVar(string name, string type, CsMemberModifier modifier, CsExpr expr = null)
		{
			Name = name;
			Type = type;
			Modifier = modifier;
			Expr = expr;
		}

		public CsVar(string name, string type, CsMemberModifier modifier, string expr)
		{
			Name = name;
			Type = type;
			Modifier = modifier;
			Expr = new CsExpr(expr);
		}
	}

    [Obsolete()]
    public class CsExpr
	{
		public string Line;
		public CsExpr(string line) { Line = line; }
	}

    [Obsolete()]
    public class CsModel
	{
		public Dictionary<string, CsType> Tis;

		public CsModel()
		{
			Tis = new Dictionary<string, CsType>();

			string[] vts = new string[]
			{
				"System.SByte,sbyte", "System.Byte,byte", "System.Int16,short", "System.UInt16,ushort",
				"System.Int32,int", "System.UInt32,uint", "System.Int64,long", "Syste.UInt64,ulong",
				"System.Singlem,float", "System.Double,double", "System.Boolean,bool", "System.String,string",
				"System.Void,void", "System.Object,object"
			};
			foreach (var vt in vts)
			{
				string[] vss = vt.Split(',');
				CsType ti = new CsType(CsTypeFlags.Value | CsTypeFlags.External, CsTypeModifier.Public, vss[0], vss[1]);
				Tis.Add(ti.FullName, ti);
			}

			vts = new string[]
			{
				"System.Collections.Generic.List", "System.Collections.Generic.Dictionary"
			};
			foreach (var vt in vts)
				RegisterExternalClass(vt);
		}

		public CsType RegisterExternalClass(string fullname)
		{
			string name = fullname.Substring(fullname.LastIndexOf('.') + 1);
			CsType ti = new CsType(CsTypeFlags.Class | CsTypeFlags.External, CsTypeModifier.Public, fullname, name);
			Tis.Add(ti.FullName, ti);
			return ti;
		}

        #region Kits

        public string GetDisplayName(CsType ti)
        {
            if (ti.NestType != null)
                return GetDisplayName(ti.NestType) + '.' + ti.Name;
            else
                return ti.Name;
        }

        public string GetDisplayName(string vt)
        {
            var ti = Tis[vt];
            return GetDisplayName(ti);
        }

        public string GetNamespace(CsType ti)
        {
            if (ti.NestType != null)
                return GetNamespace(Tis[ti.NestType]);
            else
                return ti.FullName.Substring(0, ti.FullName.LastIndexOf('.'));
        }

        #endregion
    }
}
