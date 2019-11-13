using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Reft
{
	class TestMethodAttr
	{
		Type m_ty = typeof(TestMethodAttr);

		void tf_ArgInOut(int x, ref int y, out int z)
		{
			z = y = x;
		}

		MethodInfo GetMethod(string name)
		{
			MethodInfo m = m_ty.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
			Test.Assert(m != null);
			return m;
		}

		void testParameters()
		{
			MethodInfo m = GetMethod("tf_ArgInOut");
			ParameterInfo[] prms = m.GetParameters();
			Test.Assert(prms.Length == 3);
			Test.Assert(prms[0].ParameterType.Name == "Int32" && !prms[0].IsIn && !prms[0].IsOut);
			Test.Assert(prms[1].ParameterType.Name == "Int32&" && !prms[1].IsIn && !prms[1].IsOut);
			Test.Assert(prms[2].ParameterType.Name == "Int32&" && !prms[2].IsIn && prms[2].IsOut);
		}

		public static void Run()
		{
			var o = new TestMethodAttr();
			o.testParameters();
		}
	}
}
