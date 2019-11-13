using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Nano.Ext.CodeModel.CSharp
{
    [Obsolete()]
    public class CsCodeGen
	{
        CsModel m_model;
		TextWriter m_tw;
		int m_indent;

		public CsCodeGen(CsModel model, TextWriter tw)
		{
            m_model = model;
			m_tw = tw;
			m_indent = 0;
		}

		public void WriteUsings(string[] uss)
		{
			foreach (var us in uss)
				WriteLine($"using {us};");
			if (uss.Length != 0)
				NewLine();
		}

		public void BeginNS(string ns) => BeginBlock($"namespace {ns}");

		public void EndNS() => EndBlock();

		public void WriteType(CsType ti)
		{
			switch (ti.Flags & CsTypeFlags.Mask_CT)
			{
				case CsTypeFlags.Class:
					WriteClass(ti);
					break;
				case CsTypeFlags.Delegate:
					WriteDelegate(ti);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public void WriteClass(CsType ti)
		{
			// class declaration
			string smdf = GetClassModifierText(ti.Modifiers);
			string sintf = GetBaseTypeSuffix(ti.BaseType, ti.Interfaces);
			BeginBlock($"{smdf}class {ti.Name}{sintf}");

			// fields
			foreach (var field in ti.Fields)
				WriteVar(field);
			if (ti.Fields.Count != 0)
				NewLine();

			foreach (string nestType in ti.NestedTypes)
				WriteType(m_model.Tis[nestType]);
			foreach (var mi in ti.Methods)
				WriteMethod(mi);

			EndBlock();
			NewLine();
		}

		public void WriteDelegate(CsType ti)
		{
			string smdf = GetClassModifierText(ti.Modifiers);
			Debug.Assert(ti.BaseType == null && ti.Interfaces.Length == 0);

			Debug.Assert(ti.Methods.Count == 1 && ti.Methods[0].Name == "Invoke");
			var mi = ti.Methods[0];
			var decl = MakeMethodDecl(CsMemberModifier.None, ti.Name, mi.Args, null);

			var sb = new StringBuilder(smdf);
			sb.Append("delegate ").Append(decl).Append(';');
			WriteLine(sb.ToString());
			NewLine();
		}

		void WriteMethod(CsMethod mi)
		{
			string decl;
			if (mi.Flags == CsMemberFlags.Constructor)
				decl = MakeMethodDecl(mi.Modifier, mi.Name, mi.Args, null, noRet: true);
			else
				decl = MakeMethodDecl(mi.Modifier, mi.Name, mi.Args, mi.Ret);
			BeginBlock(decl);

			foreach (var stmt in mi.Statements)
				WriteStatement(stmt);

			EndBlock();
			NewLine();
		}

		string MakeMethodDecl(CsMemberModifier modifier, string name, IList<CsVar> args, CsVar ret, bool noRet = false)
		{
			var sb = new StringBuilder();
			sb.Append(GetMemberModifierText(modifier));

			if (!noRet)
			{
				string ret_s = ret != null ? m_model.GetDisplayName(ret.Type) : "void";
				sb.Append(ret_s).Append(' ');
			}

			sb.Append(name);
			sb.Append('(');
			foreach (var arg in args)
			{
				string type = m_model.GetDisplayName(arg.Type);
				sb.Append(type).Append(' ').Append(arg.Name).Append(", ");
			}
			if (args.Count != 0)
				sb.Remove(sb.Length - 2, 2);
			sb.Append(')');

			return sb.ToString();
		}

		void WriteVar(CsVar arg)
		{
			string s = MakeVarDeclExpr(arg) + ';';
			WriteLine(s);
		}

		string MakeVarDeclExpr(CsVar arg)
		{
			string type = m_model.GetDisplayName(arg.Type);
			var modifier_s = GetMemberModifierText(arg.Modifier);
			var sb = new StringBuilder();
			sb.Append(modifier_s).Append(type).Append(' ').Append(arg.Name);
			if (arg.Expr != null)
				sb.Append(" = ").Append(arg.Expr.Line);
			return sb.ToString();
		}

		void WriteStatement(CsStatement stmt)
		{
			switch (stmt.Type)
			{
				case CsStatementType.Expression:
					WriteLine(stmt.Expr.Line + ';');
					break;
				case CsStatementType.VarDecl:
					WriteVar(stmt.Var);
					break;
				case CsStatementType.Return:
					WriteLine("return{0};", stmt.Expr != null ? ' ' + stmt.Expr.Line : null);
					break;
				case CsStatementType.If:
					WriteStatementIf(stmt);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		void WriteStatementIf(CsStatement stmt)
		{
			Debug.Assert(stmt.BrConds.Count == stmt.BrBodies.Count && stmt.BrConds.Count > 0);
			for (int i = 0; i < stmt.BrConds.Count; ++i)
			{
				var kw = i != 0 ? "else if" : "if";
				var e = $"{kw} ({stmt.BrConds[i].Line})";
				WriteStatementBody(e, stmt.BrBodies[i]);
			}

			if (stmt.Else != null)
			{
				var e = "else";
				WriteStatementBody(e, stmt.Else);
			}
		}

		void WriteStatementBody(string leader, CsStatement.Body body)
		{
			BeginBlock(leader);
			foreach (var stmt in body.Statements)
				WriteStatement(stmt);
			EndBlock();
		}

		#region Kits

		static string GetClassModifierText(CsTypeModifier m)
		{
			string s = "";
			switch (m & CsTypeModifier.Mask_Protect)
			{
				case CsTypeModifier.None:
					break;
				case CsTypeModifier.Public:
					s += "public ";
					break;
				case CsTypeModifier.Protected:
					s += "protected ";
					break;
				case CsTypeModifier.Private:
					s += "private ";
					break;
				case CsTypeModifier.Internal:
					s += "internal ";
					break;
				default:
					throw new ArgumentException();
			}
			switch (m & CsTypeModifier.Mask_Instance)
			{
				case CsTypeModifier.None:
					break;
				case CsTypeModifier.Static:
					s += "static ";
					break;
				case CsTypeModifier.Abstract:
					s += "abstract ";
					break;
				case CsTypeModifier.Final:
					s += "sealed ";
					break;
				default:
					throw new ArgumentException();
			}
			return s;
		}

		string GetBaseTypeSuffix(string baseClass, params string[] interfaces)
		{
			var sb = new StringBuilder();
			if (baseClass != null)
				sb.Append(", ").Append(m_model.GetDisplayName(baseClass));
			if (interfaces != null)
			{
				foreach (var intf in interfaces)
					sb.Append(", ").Append(m_model.GetDisplayName(intf));
			}
			if (sb.Length != 0)
				sb[0] = ':';
			return sb.ToString();
		}

		static string GetMemberModifierText(CsMemberModifier m)
		{
			string s = "";
			switch (m & CsMemberModifier.Mask_Protect)
			{
				case CsMemberModifier.None:
					break;
				case CsMemberModifier.Public:
					s += "public ";
					break;
				case CsMemberModifier.Protected:
					s += "protected ";
					break;
				case CsMemberModifier.Private:
					s += "private ";
					break;
				case CsMemberModifier.Internal:
					s += "internal ";
					break;
				default:
					throw new ArgumentException();
			}
			switch (m & CsMemberModifier.Mask_Instance)
			{
				case CsMemberModifier.None:
					break;
				case CsMemberModifier.Static:
					s += "static ";
					break;
				case CsMemberModifier.Abstract:
					s += "abstract ";
					break;
				case CsMemberModifier.Virtual:
					s += "virtual";
					break;
				case CsMemberModifier.Final:
					s += "sealed ";
					break;
				default:
					throw new ArgumentException();
			}
			if ((m & CsMemberModifier.Override) != 0)
			{
				s += "override ";
			}
			return s;
		}

		#endregion

		#region Write Kits

		void BeginBlock(string line)
		{
			WriteLine(line);
			WriteLine("{");
			++m_indent;
		}

		void EndBlock()
		{
			--m_indent;
			WriteLine("}");
		}

		void WriteLine(string s)
		{
			if (m_indent != 0)
			{
				string spaces = new string('\t', m_indent);
				m_tw.Write(spaces);
			}
			m_tw.WriteLine(s);
		}

		void WriteLine(string fmt, params object[] args)
		{
			string s = string.Format(fmt, args);
			if (m_indent != 0)
			{
				string spaces = new string('\t', m_indent);
				m_tw.Write(spaces);
			}
			m_tw.WriteLine(s);
		}

		void NewLine() => WriteLine("");

		#endregion
	}
}
