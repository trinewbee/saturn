using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Common;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace TestExt
{
	class TestJsonMarshal
	{
		class Person
		{
			public string Name;
			public int Age;

			public override bool Equals(object obj)
			{
				return CmpKit.Eq(this, (Person)obj, (a, b) => a.Name == b.Name && a.Age == b.Age);
			}
		}

		public class X
		{
			public int x;

			public X() { x = 0; }
			public X(int _x) { x = _x; }

			public static bool EqX(X a, X b) => a.x == b.x;
		}

		public class Y
		{
			public int a;
			public X b;
			public Dictionary<string, List<X>> c;

			public static bool EqY(Y a, Y b) => a.a == b.a && CmpKit.Eq(a.b, b.b, X.EqX) && CmpKit.Eq(a.c, b.c, EqDLX);

			static bool EqDLX(Dictionary<string, List<X>> a, Dictionary<string, List<X>> b) => CmpKit.DictEq(a, b, EqLX);

			static bool EqLX(List<X> a, List<X> b) => CmpKit.ListEq(a, b, X.EqX);
		}

		Person p1 = new Person() { Name = "Yang", Age = 1 };
		Person p2 = new Person() { Name = "Wang", Age = 2 };
		Person p3 = new Person() { Name = null, Age = 0 };

		void testValueNode()
		{
			string s;

			s = JsonMarshal.Dumps(byte.MaxValue);
			Test.Assert(s == "255");
			Test.Assert(JsonMarshal.Loads<byte>(s) == 255);

			s = JsonMarshal.Dumps(sbyte.MinValue);
			Test.Assert(s == "-128");
			Test.Assert(JsonMarshal.Loads<sbyte>(s) == -128);

			s = JsonMarshal.Dumps(short.MinValue);
			Test.Assert(s == "-32768");
			Test.Assert(JsonMarshal.Loads<short>(s) == -32768);

			s = JsonMarshal.Dumps(ushort.MaxValue);
			Test.Assert(s == "65535");
			Test.Assert(JsonMarshal.Loads<ushort>(s) == 65535);

			s = JsonMarshal.Dumps(int.MinValue);
			Test.Assert(s == "-2147483648");
			Test.Assert(JsonMarshal.Loads<int>(s) == -2147483648);

			s = JsonMarshal.Dumps(uint.MaxValue);
			Test.Assert(s == "4294967295");
			Test.Assert(JsonMarshal.Loads<uint>(s) == 4294967295);

			s = JsonMarshal.Dumps(long.MinValue);
			Test.Assert(s == "-9223372036854775808");
			Test.Assert(JsonMarshal.Loads<long>(s) == -9223372036854775808);

			s = JsonMarshal.Dumps(ulong.MaxValue);
			Test.Assert(s == "-1");
			Test.Assert(JsonMarshal.Loads<ulong>(s) == 18446744073709551615);

			s = JsonMarshal.Dumps(true);
			Test.Assert(s == "true" && JsonMarshal.Loads<bool>(s));
			s = JsonMarshal.Dumps(false);
			Test.Assert(s == "false" && !JsonMarshal.Loads<bool>(s));

			s = JsonMarshal.Dumps(1.23f);
			Test.Assert(JsonMarshal.Loads<float>(s) == 1.23f);
			s = JsonMarshal.Dumps(1.0f);
			Test.Assert(JsonMarshal.Loads<float>(s) == 1.0f);	// int node

			s = JsonMarshal.Dumps(1.23d);
			Test.Assert(JsonMarshal.Loads<double>(s) == 1.23d);
			s = JsonMarshal.Dumps(1.0d);
			Test.Assert(JsonMarshal.Loads<double>(s) == 1.0d);	// int node

			s = JsonMarshal.Dumps("1");
			Test.Assert(s == "\"1\"");
			Test.Assert(JsonMarshal.Loads<string>(s) == "1");

			Test.Assert(JsonMarshal.Dumps(null) == "null");
			Test.Assert(JsonMarshal.Loads<string>("null") == null);
		}

		void testSimpleObjectNode()
		{
			string s = "{\"Name\":\"Yang\",\"Age\":1}";
			Test.Assert(JsonMarshal.Dumps(p1) == s);
			Person p = JsonMarshal.Loads<Person>(s);
			Test.Assert(p.Equals(p1));

			s = "{\"Name\":null,\"Age\":0}";
			Test.Assert(JsonMarshal.Dumps(p3) == s);
			p = JsonMarshal.Loads<Person>(s);
			Test.Assert(p.Equals(p3));

			Test.Assert(JsonMarshal.Loads<Person>("null") == null);
		}

		void testSimpleListNode()
		{
			var vs = new List<int> { 1, 2, 3 };
			string s = "[1,2,3]";
			Test.Assert(JsonMarshal.Dumps(vs) == s);
			vs = JsonMarshal.Loads<List<int>>(s);
			Test.Assert(vs.Count == 3 && vs[0] == 1 && vs[2] == 3);

			var ps = new List<Person> { p1, p2 };
			s = "[{\"Name\":\"Yang\",\"Age\":1},{\"Name\":\"Wang\",\"Age\":2}]";
			Test.Assert(JsonMarshal.Dumps(ps) == s);

			ps = JsonMarshal.Loads<List<Person>>(s);
			Test.Assert(ps.Count == 2 && ps[0].Equals(p1) && ps[1].Equals(p2));			

			Test.Assert(JsonMarshal.Loads<List<Person>>("null") == null);
		}

		void testSimpleDictNode()
		{
			var vs = new Dictionary<string, int> { { "1", 1 }, { "2", 2 }, { "3", 3 } };
			string s = "{\"1\":1,\"2\":2,\"3\":3}";
			Test.Assert(JsonMarshal.Dumps(vs) == s);
			vs = JsonMarshal.Loads<Dictionary<string, int>>(s);
			Test.Assert(vs.Count == 3 && vs["1"] == 1 && vs["3"] == 3);

			var ps = new Dictionary<string, Person> { { p1.Name, p1 }, { p2.Name, p2 } };
			s = "{\"Yang\":{\"Name\":\"Yang\",\"Age\":1},\"Wang\":{\"Name\":\"Wang\",\"Age\":2}}";
			Test.Assert(JsonMarshal.Dumps(ps) == s);

			ps = JsonMarshal.Loads<Dictionary<string, Person>>(s);
			Test.Assert(ps.Count == 2 && ps["Yang"].Equals(p1) && ps["Wang"].Equals(p2));

			Test.Assert(JsonMarshal.Loads<Dictionary<string, Person>>("null") == null);
		}

		void testComplexNode()
		{
			X x1 = new X(1), x2 = new X(2), x3 = new X(3), x4 = new X(4), x5 = new X(5);
			List<X> l1 = new List<X> { x2, x3 }, l2 = new List<X> { x4, x5 };
			Y y = new Y()
			{
				a = 0,
				b = x1,
				c = new Dictionary<string, List<X>> { { "1", l1 }, { "2", l2 } }
			};
			string s = JsonMarshal.Dumps(y);
			Test.Assert(s == "{\"a\":0,\"b\":{\"x\":1},\"c\":{\"1\":[{\"x\":2},{\"x\":3}],\"2\":[{\"x\":4},{\"x\":5}]}}");
			Y yt = JsonMarshal.Loads<Y>(s);
			Test.Assert(CmpKit.Eq(y, yt, Y.EqY));

			List<Y> ys = new List<Y> { y, null };
			s = JsonMarshal.Dumps(ys);
			Test.Assert(s == "[{\"a\":0,\"b\":{\"x\":1},\"c\":{\"1\":[{\"x\":2},{\"x\":3}],\"2\":[{\"x\":4},{\"x\":5}]}},null]");
			List<Y> yst = JsonMarshal.Loads<List<Y>>(s);
			Test.Assert(yst.Count == 2 && CmpKit.Eq(yst[0], y, Y.EqY) && yst[1] == null);
			Test.Assert(CmpKit.ListEq(ys, yst, Y.EqY));
		}

		public static void Run()
		{
			Console.WriteLine("TestMarshal.TestJsonMarshal");
			var o = new TestJsonMarshal();
			o.testValueNode();
			o.testSimpleObjectNode();
			o.testSimpleListNode();
			o.testSimpleDictNode();
			o.testComplexNode();
		}
	}
}
