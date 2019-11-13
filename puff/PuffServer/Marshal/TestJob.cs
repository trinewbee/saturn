using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nano.Json;
using Nano.Nuts;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace Puff.Marshal
{
    class JMT_X
    {
        public string T;
        public int X;

        public void f(string t, int x) { }
    }

    class TestJsonObjectBuilder
    {
        JsonObjectBuilder job = JsonObjectBuilder.BuildDefault();

        void testPrimitive()
        {
            var jn = new JsonNode(JsonNodeType.Null);
            Test.Assert(job.Parse(typeof(JMT_X), jn) == null);
            Test.Assert(job.Parse(typeof(string), jn) == null);
            Test.AssertException(() => job.Parse(typeof(int), jn), typeof(NutsException));

            Test.Assert((char)job.Parse(typeof(char), _int(0x30)) == '0');
            Test.Assert((sbyte)job.Parse(typeof(sbyte), _int(-128)) == -128);
            Test.Assert((byte)job.Parse(typeof(byte), _int(255)) == 255);
            Test.Assert((short)job.Parse(typeof(short), _int(-32768)) == -32768);
            Test.Assert((ushort)job.Parse(typeof(ushort), _int(65535)) == 65535);
            Test.Assert((int)job.Parse(typeof(int), _int(-2147483648)) == -2147483648);
            Test.Assert((uint)job.Parse(typeof(uint), _int(4294967295)) == 4294967295);
            Test.Assert((long)job.Parse(typeof(long), _int(-9223372036854775808)) == -9223372036854775808);
            Test.Assert((ulong)job.Parse(typeof(ulong), _int(-1)) == 18446744073709551615);

            jn = new JsonNode(1.23);
            Test.Assert((float)job.Parse(typeof(float), jn) == 1.23f);
            Test.Assert((double)job.Parse(typeof(double), jn) == 1.23d);
            Test.Assert((float)job.Parse(typeof(float), _int(-2)) == -2f);
            Test.Assert((double)job.Parse(typeof(double), _int(-2)) == -2d);

            Test.Assert((bool)job.Parse(typeof(bool), new JsonNode(true)));
            Test.Assert(!(bool)job.Parse(typeof(bool), new JsonNode(false)));

            Test.Assert((string)job.Parse(typeof(string), _str("Hello")) == "Hello");
            Test.Assert((string)job.Parse(typeof(string), _str("")) == "");
            Test.Assert(job.Parse(typeof(string), new JsonNode(JsonNodeType.Null)) == null);

            jn = JsonModel.Dump("abc");
            Test.Assert((JsonNode)job.Parse(typeof(JsonNode), jn) == jn);
            jn = JsonModel.Dump(new List<object> { "abc", 123 });
            Test.Assert((JsonNode)job.Parse(typeof(JsonNode), jn) == jn);
        }

        enum Color
        {
            Red, Green, Blue
        }

        void testEnum()
        {
            var c = (Color)job.Parse(typeof(Color), new JsonNode("Green"));
            Test.Assert(c == Color.Green);

            c = (Color)job.Parse(typeof(Color), new JsonNode("2"));
            Test.Assert(c == Color.Blue);

            var w = (DayOfWeek)job.Parse(typeof(DayOfWeek), new JsonNode(4));
            Test.Assert(w == DayOfWeek.Thursday);
        }

        void testClass()
        {
            object o = new Dictionary<string, object> { { "T", "abc" }, { "X", 123 } };
            var jn = JsonModel.Dump(o);
            JMT_X x = (JMT_X)job.Parse(typeof(JMT_X), jn);
            Test.Assert(x.T == "abc" && x.X == 123);
        }

        void testArray()
        {
            object o = new List<object> { "abc", "123" };
            var jn = JsonModel.Dump(o);
            var x = (string[])job.Parse(typeof(string[]), jn);
            Test.Assert(x.Length == 2 && x[0] == "abc" && x[1] == "123");
        }

        void testList()
        {
            object o = new List<object> { "abc", "123" };
            var jn = JsonModel.Dump(o);
            var x = (List<string>)job.Parse(typeof(List<string>), jn);
            Test.Assert(x.Count == 2 && x[0] == "abc" && x[1] == "123");
        }

        void testDictionary()
        {
            object o = new Dictionary<string, object> { { "a", "abc" }, { "b", "123" } };
            var jn = JsonModel.Dump(o);
            var x = (Dictionary<string, string>)job.Parse(typeof(Dictionary<string, string>), jn);
            Test.Assert(x.Count == 2 && x["a"] == "abc" && x["b"] == "123");
        }

        void testNullable()
        {
            Test.Assert((char?)job.Parse(typeof(char?), _int(0x30)) == '0');
            Test.Assert((sbyte?)job.Parse(typeof(sbyte?), _int(-128)) == -128);
            Test.Assert((byte?)job.Parse(typeof(byte?), _int(255)) == 255);
            Test.Assert((short?)job.Parse(typeof(short?), _int(-32768)) == -32768);
            Test.Assert((ushort?)job.Parse(typeof(ushort?), _int(65535)) == 65535);
            Test.Assert((int?)job.Parse(typeof(int?), _int(-2147483648)) == -2147483648);
            Test.Assert((uint?)job.Parse(typeof(uint?), _int(4294967295)) == 4294967295);
            Test.Assert((long?)job.Parse(typeof(long?), _int(-9223372036854775808)) == -9223372036854775808);
            Test.Assert((ulong?)job.Parse(typeof(ulong?), _int(-1)) == 18446744073709551615);

            var jn = new JsonNode(1.23);
            Test.Assert((float?)job.Parse(typeof(float?), jn) == 1.23f);
            Test.Assert((double?)job.Parse(typeof(double?), jn) == 1.23d);
            Test.Assert((float?)job.Parse(typeof(float?), _int(-2)) == -2f);
            Test.Assert((double?)job.Parse(typeof(double?), _int(-2)) == -2d);

            Test.Assert((bool?)job.Parse(typeof(bool?), new JsonNode(true)) == true);
            Test.Assert((bool?)job.Parse(typeof(bool?), new JsonNode(false)) == false);

            jn = new JsonNode(JsonNodeType.Null);
            Test.Assert(job.Parse(typeof(char?), jn) == null);
            Test.Assert(job.Parse(typeof(sbyte?), jn) == null);
            Test.Assert(job.Parse(typeof(byte?), jn) == null);
            Test.Assert(job.Parse(typeof(short?), jn) == null);
            Test.Assert(job.Parse(typeof(ushort?), jn) == null);
            Test.Assert(job.Parse(typeof(int?), jn) == null);
            Test.Assert(job.Parse(typeof(uint?), jn) == null);
            Test.Assert(job.Parse(typeof(long?), jn) == null);
            Test.Assert(job.Parse(typeof(ulong?), jn) == null);
            Test.Assert(job.Parse(typeof(float?), jn) == null);
            Test.Assert(job.Parse(typeof(double?), jn) == null);
            Test.Assert(job.Parse(typeof(bool?), jn) == null);
        }

        #region Make node

        JsonNode _int(long v) => new JsonNode(v);

        JsonNode _str(string v) => new JsonNode(v);

        static bool FloatEqual(float a, float b) => Math.Abs((a - b) / (a + b)) < 1e-6;

        static bool DoubleEqual(double a, double b) => Math.Abs((a - b) / (a + b)) < 1e-12;

        #endregion

        public static void Run()
        {
            Console.WriteLine(nameof(TestJsonObjectBuilder));
            var o = new TestJsonObjectBuilder();
            o.testPrimitive();
            o.testEnum();
            o.testClass();
            o.testArray();
            o.testList();
            o.testDictionary();
            o.testNullable();
        }
    }
}
