using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Ext.CodeModel;
using Nano.UnitTest;
using System.Linq.Expressions;

namespace TestExt
{
    class TestLEM
    {
        delegate int d1(int x);
        delegate int d2(int x, int y);

        #region tester

        static void t_expr_0(LEM m, int value)
        {
            var f = m.Compile<Func<int>>();
            Test.Assert(f() == value);
        }

        static void t_expr_0(LEM m, bool value)
        {
            var f = m.Compile<Func<bool>>();
            Test.Assert(f() == value);
        }

        static void t_expr_1(LEM m, LEM.Argx args, int a, int value)
        {
            var f = m.Compile<d1>(args);
            Test.Assert(f(a) == value);
        }

        static void t_expr_2(LEM m, LEM.Argx args, int a, int b, int value)
        {
            var f = m.Compile<d2>(args);
            Test.Assert(f(a, b) == value);
        }

        #endregion

        void testArithExpr()
        {
            LEM m = LEM.Value(1) + LEM.Value(2);
            t_expr_0(m, 3);

            m = LEM.Value(1) + LEM.Value(2) * (LEM.Value(6) - LEM.Value(3));
            t_expr_0(m, 7);
        }

		void testComparisonExpr()
		{
			// >
			LEM m = LEM.Value(2) > LEM.Value(1);
			t_expr_0(m, true);

			m = LEM.Value(2) > LEM.Value(2);
			t_expr_0(m, false);

			// >=
			m = LEM.Value(2) >= LEM.Value(1);
			t_expr_0(m, true);

			m = LEM.Value(2) >= LEM.Value(2);
			t_expr_0(m, true);

			m = LEM.Value(2) >= LEM.Value(3);
			t_expr_0(m, false);

			// <
			m = LEM.Value(2) < LEM.Value(3);
			t_expr_0(m, true);

			m = LEM.Value(2) < LEM.Value(2);
			t_expr_0(m, false);

			// <=
			m = LEM.Value(2) <= LEM.Value(3);
			t_expr_0(m, true);

			m = LEM.Value(2) <= LEM.Value(2);
			t_expr_0(m, true);

			m = LEM.Value(2) <= LEM.Value(1);
			t_expr_0(m, false);

			// ==
			m = LEM.Value(1) == LEM.Value(1);
			t_expr_0(m, true);

			m = LEM.Value(1) == LEM.Value(2);
			t_expr_0(m, false);

			// !=
			m = LEM.Value(1) != LEM.Value(1);
			t_expr_0(m, false);

			m = LEM.Value(1) != LEM.Value(2);
			t_expr_0(m, true);
		}

		void testLogicExpr()
        {
            var m = LEM.Value(6) & LEM.Value(3);
            t_expr_0(m, 2);

            m = LEM.Value(6) | LEM.Value(3);
            t_expr_0(m, 7);

			m = LEM.Value(6) ^ LEM.Value(3);
			t_expr_0(m, 5);

			m = LEM.And(LEM.Value(true), LEM.Value(true));
            t_expr_0(m, true);

            m = LEM.And(LEM.Value(true), LEM.Value(false));
            t_expr_0(m, false);

            m = LEM.Or(LEM.Value(true), LEM.Value(false));
            t_expr_0(m, true);

            m = LEM.Or(LEM.Value(false), LEM.Value(false));
            t_expr_0(m, false);

			m = LEM.And(LEM.Value(2) > LEM.Value(1), LEM.Value(3) < LEM.Value(4));
			t_expr_0(m, true);
		}

		class fc
		{
			public int a = 10;
			public int add(int x) => x + a;
			public static int sadd(int x, int y) => x + y;
		}

		void testFuncCall()
		{
			var vt = typeof(fc);
			var mi1 = vt.GetMethod("add");
			var mi2 = vt.GetMethod("sadd");
			Test.Assert(mi1 != null && mi2 != null);

			var o = new fc();
			var mo = LEM.Value(o);

			var m = mo.Call(mi1, LEM.Value(1));
			t_expr_0(m, 11);
			m = LEM.Call(mo, mi1, LEM.Value(1));
			t_expr_0(m, 11);

			m = LEM.Call(null, mi2, LEM.Value(2), LEM.Value(3));
			t_expr_0(m, 5);

			m = mo.Call((Func<int, int>)o.add, LEM.Value(1));
			t_expr_0(m, 11);
			m = LEM.Call(mo, (Func<int, int>)o.add, LEM.Value(1));
			t_expr_0(m, 11);

			m = LEM.Call(null, (Func<int, int, int>)fc.sadd, LEM.Value(2), LEM.Value(3));
			t_expr_0(m, 5);
			m = LEM.Call(null, new Func<int, int, int>(fc.sadd), LEM.Value(2), LEM.Value(3));
			t_expr_0(m, 5);
		}

		class ftc
		{
			public int X = 3;
			private int Y = 5;
			private static int Z = 8;
		}

		delegate int ftcd(ftc o);

		void testField()
		{
			var o = new ftc();
			var args = LEM.Args<ftcd>();
			LEM m = LEM.Field(LEM.Value(o), typeof(ftc), "X");			
			var f = m.Compile<ftcd>(args);
			Test.Assert(f(o) == 3);

			m = LEM.Field(LEM.Value(o), typeof(ftc), "Y");
			f = m.Compile<ftcd>(args);
			Test.Assert(f(o) == 5);

#if false
			m = LEM.Field(LEM.Value(null), typeof(ftc), "Z");
			f = m.Compile<ftcd>(args);
			Test.Assert(f(o) == 8);
#endif
		}

		void testParameters()
        {            
            var args = LEM.Args<d2>();
            LEM m = args["x"] + args["y"];
            t_expr_2(m, args, 1, 2, 3);
        }

        void testLocalVars()
        {
            int y = 2;
            var args = LEM.Args<d1>();
            LEM m = args["x"] + LEM.Value(y);
            t_expr_1(m, args, 1, 3);
        }

		void testJoin()
		{
			Expression<Func<int, bool>> f1 = x => x > 1;
			Expression<Func<int, bool>> f2 = x => x < 3;
			var map = LEM.BuildArgMap(f1);
			var m1 = LEM.Import(f1, map);
			var m2 = LEM.Import(f2, map);
			var args = new ParameterExpression[] { map["x"] };
			var m = LEM.And(m1, m2);
			var f = m.Compile<Func<int, bool>>(args);
			Test.Assert(!f(1) && f(2) && !f(3));
		}

        public static void Run()
        {
            Console.WriteLine("TestLEM");
            var o = new TestLEM();
            o.testArithExpr();
			o.testComparisonExpr();
			o.testLogicExpr();
			o.testFuncCall();
			o.testField();
			o.testParameters();
            o.testLocalVars();
			o.testJoin();
        }
    }
}
