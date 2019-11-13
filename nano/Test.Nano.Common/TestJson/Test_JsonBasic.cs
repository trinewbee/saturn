using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
    class Test_JsonBasic
    {
		static void assertFalse(bool f) => Test.Assert(!f);

		static void assertStringEqual(string x, string y) => Test.Assert(string.CompareOrdinal(x, y) == 0);

		static void testBasic()
        {
            JNode jbvTrue = true;
            Test.Assert(jbvTrue.IsBoolean);
            Test.Assert(jbvTrue);
            Test.Assert((bool)jbvTrue == true);
            Test.Assert(jbvTrue == true);
            Test.Assert(jbvTrue.BoolValue);
			assertFalse(!jbvTrue);

            JNode jbvFalse = false;
            Test.Assert(jbvFalse.IsBoolean);
			assertFalse(jbvFalse);
            Test.Assert((bool)jbvFalse == false);
            Test.Assert(jbvFalse == false);
            assertFalse(jbvFalse.BoolValue);
            Test.Assert(!jbvFalse);

            JNode jbv1 = (byte)1;
            Test.Assert(jbv1.IsInteger);
            Test.Assert(jbv1.IntValue == 1);
            Test.Assert(jbv1 == (byte)1);
            Test.Assert((byte)jbv1 == (byte)1);

            JNode jsb1 = (sbyte)1;
            Test.Assert(jsb1.IsInteger);
            Test.Assert(jsb1.IntValue == 1);
            Test.Assert(jsb1 == (sbyte)1);
            Test.Assert((sbyte)jsb1 == (byte)1);

            JNode jsv = (short)1;
            Test.Assert(jsv.IsInteger);
            Test.Assert(jsv.IntValue == 1);
            Test.Assert(jsv == (short)1);
            Test.Assert((short)jsv == (short)1);

            JNode jus = (ushort)1;
            Test.Assert(jus.IsInteger);
            Test.Assert(jus.IntValue == 1);
            Test.Assert(jus == (ushort)1);
            Test.Assert((ushort)jus == (ushort)1);

            JNode jnv = (int)1;
            Test.Assert(jnv.IsInteger);
            Test.Assert(jnv.IntValue == 1);
            Test.Assert(jnv == (int)1);
            Test.Assert((int)jnv == (int)1);

            JNode jun = (uint)1;
            Test.Assert(jun.IsInteger);
            Test.Assert(jun.IntValue == 1);
            Test.Assert(jun == (uint)1);
            Test.Assert((uint)jun == (uint)1);

            JNode jlv = (long)1;
            Test.Assert(jlv.IsInteger);
            Test.Assert(jlv.IntValue == 1);
            Test.Assert(jlv == (long)1);
            Test.Assert((long)jlv == (long)1);
            //for all
            Test.Assert((byte)jlv == (byte)1);
            Test.Assert((sbyte)jlv == (sbyte)1);
            Test.Assert((char)jlv == (char)1);
            Test.Assert((short)jlv == (short)1);
            Test.Assert((ushort)jlv == (ushort)1);
            Test.Assert((int)jlv == (int)1);
            Test.Assert((uint)jlv == (uint)1);
            Test.Assert((long)jlv == (long)1);
            Test.Assert((ulong)jlv == (ulong)1);
            Test.Assert((float)jlv == (float)1);
            Test.Assert((double)jlv == (float)1);

            JNode jul = (ulong)1;
            Test.Assert(jul.IsInteger);
            Test.Assert(jul.IntValue == 1);
            Test.Assert(jul == (ulong)1);
            Test.Assert((ulong)jul == (ulong)1);

            JNode jfv = (float)1.0f;
            Test.Assert(jfv.IsFloat);
            Test.Assert(jfv.FloatValue == 1.0f);
            Test.Assert(jfv == (float)1);
            Test.Assert((float)jfv == (float)1.0f);

            JNode jdv = (double)1.0;
            Test.Assert(jdv.IsFloat);
            Test.Assert(jdv.FloatValue == 1.0);
            Test.Assert(jdv == (double)1.0);
            Test.Assert((double)jdv == (double)1.0);

            JNode jstr = "1";
            Test.Assert(jstr.IsString);
            Test.Assert(jstr.TextValue == "1");
            Test.Assert(jstr == "1");
            Test.Assert((string)jstr == "1");
        }

        static void testArray()
        {
            JNode v;
            JNode arr = new object[] { 1, "2", true, new {} };
            Test.Assert(arr.IsArray);
            Test.Assert(arr.Length == 4);
            Test.AssertExceptionType<TypeException>(() => Console.Write(arr.Count));
			Test.AssertExceptionType<TypeException>(() => {
                foreach (var a in arr.Fields)
                    Console.Write(a.Value.ToString());
            });

            //get
            arr = new object[] { 1, "2", true, new { } };
            Test.Assert(arr[0] == 1);

			Test.AssertExceptionType<ArgumentOutOfRangeException>(() => Console.Write("" + arr[11]));
			Test.AssertExceptionType<TypeException>(() => Console.Write("" + arr.TryGet("11", out v)));
			Test.AssertExceptionType<TypeException>(() => Console.Write("" + arr["11"]));

            //set
            arr = new object[] { 1, "2", true, new { } };
            arr[0] = "b";
            Test.Assert(arr[0].IsString && arr[0].TextValue == "b");
            arr[1] = JNode.Undefined;
            Test.Assert(arr.Length == 3);
            Test.Assert(arr[1] == true);
			Test.AssertExceptionType<ArgumentOutOfRangeException>(() => arr[100] = "c");
			Test.AssertExceptionType<TypeException>(() => arr.TryAdd("100", "xx"));

            //add
            arr = new object[] { 1, "2", true, new { } };
            arr.Add("last1");
            assertStringEqual("last1", arr[arr.Length - 1]);
			Test.AssertExceptionType<TypeException>(() => arr.Add(JNode.Undefined));

            arr = new object[] { 1, "2", true, new { } };
            v = arr[3];
            arr.RemoveItem(1);
            arr.RemoveItem("2");
            arr.RemoveItem(true);
            arr.RemoveItem(JNode.Create(new { }));
            arr.RemoveItem(v);

            arr = new object[] { 1, "2", true, new { } };
            arr.RemoveAt(1);
            arr.RemoveAt(2);

            //enum
            arr = new object[] { 1, "2", true, new { } };
            var i = 0;
            var sb = new StringBuilder();
            foreach (var it in arr.Items)
            {
                i++;
                sb.Append(it.ToString());
            }
            Test.Assert(i == 4);
            assertStringEqual("Integer(1)String(2)Boolean(True)Object[0]", sb.ToString());
        }

        static void testObject()
        {
            JNode obj = JNode.Create(new { a = 1, b = true, c = "c", d=new { } });
            Test.Assert(obj.IsObject && obj.Count == 4);
			Test.AssertExceptionType<TypeException>(() => Console.Write(obj.Length));
			Test.AssertExceptionType<TypeException>(() => Console.Write(obj.Items));

            //get
            JNode v = obj["a"];
            obj = JNode.Create(new { a = 1, b = true, c = "c", d = new { } });
            Test.Assert(obj["a"] == 1);
            Test.Assert(obj.TryGet("c", out v));
            Test.Assert(v == "c");
            Test.Assert(obj["111"].IsUndefined);
			Test.AssertExceptionType<TypeException>(() => Console.Write("" + obj[1111]));

            //put
            obj = JNode.Create(new { a = 1, b = true, c = "c", d = new { } });
            obj["a"] = "a_1";
            Test.Assert(obj["a"].TextValue == "a_1");
            obj["key"] = "txt";
            Test.Assert(obj["key"].TextValue == "txt");
            Test.Assert(obj.Count == 5);

            obj.TryAdd("key3", "txt3");
            Test.Assert(obj["key3"].TextValue == "txt3");
            Test.Assert(obj.Count == 6);
            assertFalse(obj.TryAdd("c", "c_1"));
            Test.Assert(obj["c"].TextValue == "c");

            //remove
            obj = JNode.Create(new { a = 1, b = true, c = "c", d = new { } });
            obj["a"] = JNode.Undefined;
            Test.Assert(obj["a"].IsUndefined);
            Test.Assert(obj.Count == 3);

            obj.Remove("b");
            Test.Assert(obj["b"].IsUndefined);
            Test.Assert(obj.Count == 2);

            obj.TryRemove("c", out v);
            Test.Assert(v == "c");
            Test.Assert(obj["c"].IsUndefined);
            Test.Assert(obj.Count == 1);

            //enum
            obj = JNode.Create(new { a = 1, b = true, c = "c", d = new { } });
#if NETFX
            var i = 0;
            var sb = new StringBuilder();
            foreach (var f in obj.Fields)
            {
                i++;
                sb.Append(f.Value.ToString());
            }
            Test.Assert(i == 4);
            assertStringEqual("Object[0]Integer(1)String(c)Boolean(True)", sb.ToString());
#endif
        }

        public static void Run()
        {
            Console.WriteLine("Tests.JNode.Net.Test_JNodeBasic");
            testBasic();
            testArray();
            testObject();
        }
    }
}
