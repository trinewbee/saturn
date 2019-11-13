using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Reft
{
	static class StcClass
	{
		public static int m_stx;
	}

	abstract class AbsClass
	{
		public static int m_stx;
	}

	sealed class SeaClass
	{
		public static int m_stx;
	}

	enum TestEnum
	{
	}

	struct TestStruct
	{
	}

	interface TestInterface
	{
	}

	class TestClass
	{
		public delegate void Do();

		public static int m_stx;
	}

	class TestClassN
	{
	}

	class TestClassAttr
	{
		Type m_tyStc = typeof(StcClass);
		Type m_tyAbs = typeof(AbsClass);
		Type m_tySea = typeof(SeaClass);
		Type m_tyTc = typeof(TestClass);
		Type m_tyTcN = typeof(TestClassN);
		Type m_tyTi = typeof(TestInterface);
		Type m_tyTs = typeof(TestStruct);
		Type m_tyTe = typeof(TestEnum);
		Type m_tyDg = typeof(TestClass.Do);

		void testName()
		{
			string prefix = "Reft.";
			Test.Assert(m_tyTc.FullName == prefix + "TestClass");
			Test.Assert(m_tyTi.FullName == prefix + "TestInterface");
			Test.Assert(m_tyTs.FullName == prefix + "TestStruct");
			Test.Assert(m_tyTe.FullName == prefix + "TestEnum");
			Test.Assert(m_tyDg.FullName == prefix + "TestClass+Do");
		}

		void testClassify()
		{
			Test.Assert(m_tyTc.IsClass && !m_tyTc.IsInterface && !m_tyTc.IsEnum && !m_tyTc.IsValueType && !m_tyTc.IsPrimitive);
			Test.Assert(!m_tyTi.IsClass && m_tyTi.IsInterface && !m_tyTi.IsEnum && !m_tyTi.IsValueType && !m_tyTi.IsPrimitive);
			Test.Assert(!m_tyTs.IsClass && !m_tyTs.IsInterface && !m_tyTs.IsEnum && m_tyTs.IsValueType && !m_tyTs.IsPrimitive);
			Test.Assert(!m_tyTe.IsClass && !m_tyTe.IsInterface && m_tyTe.IsEnum && m_tyTe.IsValueType && !m_tyTe.IsPrimitive);
			Test.Assert(m_tyDg.IsClass && !m_tyDg.IsInterface && !m_tyDg.IsEnum && !m_tyDg.IsValueType && !m_tyDg.IsPrimitive);
			Type tyS = typeof(string);
			Test.Assert(tyS.IsClass && !tyS.IsInterface && !tyS.IsEnum && !tyS.IsValueType && !tyS.IsPrimitive);
			Type tyI = typeof(int);
			Test.Assert(!tyI.IsClass && !tyI.IsInterface && !tyI.IsEnum && tyI.IsValueType && tyI.IsPrimitive);
		}

		void testModifier()
		{
			Test.Assert(m_tyStc.Attributes == (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit));
			Test.Assert(m_tyAbs.Attributes == (TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit));
			Test.Assert(m_tySea.Attributes == (TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit));
			Test.Assert(m_tyTc.Attributes == (TypeAttributes.BeforeFieldInit));
			Test.Assert(m_tyTcN.Attributes == (TypeAttributes.BeforeFieldInit));
		}

		public static void Run()
		{
			var o = new TestClassAttr();
			o.testName();
			o.testClassify();
			o.testModifier();
		}
	}
}
