using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nano.Json;
using Nano.Ext.Marshal;
using Nano.UnitTest;

namespace Puff.Marshal
{
    class TestJsonModelBuilder
    {
        JsonModelBuilder jmb = JsonModelBuilder.BuildDefault();

        void testPrimitive()
        {
            Test.Assert(jmb.Build(null).NodeType == JsonNodeType.Null);

            _test_int_node('0', 0x30);
            _test_int_node(byte.MaxValue, 255);
            _test_int_node(sbyte.MinValue, -128);
            _test_int_node(ushort.MaxValue, 65535);
            _test_int_node(short.MinValue, -32768);
            _test_int_node(uint.MaxValue, 4294967295);
            _test_int_node(int.MinValue, -2147483648);
            _test_int_node(ulong.MaxValue, -1); // 18446744073709551615
            _test_int_node(long.MinValue, -9223372036854775808);

            _test_dbl_node(1.23f, 1.23, false);
            _test_dbl_node(1.23d, 1.23, true);

            _test_bool_node(true, true);
            _test_bool_node(false, false);

            _test_str_node("Hello", "Hello");
            _test_str_node("", "");

            var jn = JsonModel.Dump("abc");
            _test_str_node(jn, "abc");

            jn = JsonModel.Dump(new List<object> { "abc", 123 });
            jn = jmb.Build(jn);
            Test.Assert(jn.ChildCount == 2 && jn[0].TextValue == "abc" && jn[1].IntValue == 123);
        }

        void testClass()
        {
            object o = new JMT_X { X = 123, T = "abc" };
            var jn = jmb.Build(o);
            Test.Assert(jn.ChildCount == 2 && jn["T"].TextValue == "abc" && jn["X"].IntValue == 123);

            o = new { a = "abc", b = 123 };
            jn = jmb.Build(o);
            Test.Assert(jn.ChildCount == 2 && jn["a"].TextValue == "abc" && jn["b"].IntValue == 123);
        }

        void testArray()
        {
            var ls = new object[] { "abc", 123 };
            var jn = jmb.Build(ls);
            Test.Assert(jn.ChildCount == 2 && jn[0].TextValue == "abc" && jn[1].IntValue == 123);
        }

        enum Color
        {
            Red, Green, Blue
        }

        void testEnum()
        {
            var jn = jmb.Build(Color.Green);
            Test.Assert(jn.NodeType == JsonNodeType.Integer && jn.IntValue == 1);

            jn = jmb.Build(Color.Blue);
            Test.Assert(jn.NodeType == JsonNodeType.Integer && jn.IntValue == 2);

            jn = jmb.Build(DayOfWeek.Thursday);
            Test.Assert(jn.NodeType == JsonNodeType.Integer && jn.IntValue == 4);
        }

        void testList()
        {
            var ls = new List<object> { "abc", 123 };
            var jn = jmb.Build(ls);
            Test.Assert(jn.ChildCount == 2 && jn[0].TextValue == "abc" && jn[1].IntValue == 123);
        }

        void testDictionary()
        {
            var dc = new Dictionary<string, object> { { "a", "abc" }, { "b", 123 } };
            var jn = jmb.Build(dc);
            Test.Assert(jn.ChildCount == 2 && jn["a"].TextValue == "abc" && jn["b"].IntValue == 123);
        }

        #region Test node

        void _test_int_node(object o, long v)
        {
            var jnode = jmb.Build(o);
            Test.Assert(jnode.NodeType == JsonNodeType.Integer && jnode.IntValue == v);
        }

        static bool FloatEqual(double a, double b, double precise) => Math.Abs((a - b) / (a + b)) < precise;

        void _test_dbl_node(object o, double v, bool isdbl)
        {
            var jnode = jmb.Build(o);
            double precise = isdbl ? 1e-12 : 1e-6;
            Test.Assert(jnode.NodeType == JsonNodeType.Float && FloatEqual(jnode.FloatValue, v, precise));
        }

        void _test_bool_node(object o, bool v)
        {
            var jnode = jmb.Build(o);
            Test.Assert(jnode.NodeType == JsonNodeType.Boolean && jnode.BoolValue == v);
        }

        void _test_str_node(object o, string v)
        {
            var jnode = jmb.Build(o);
            Test.Assert(jnode.NodeType == JsonNodeType.String && jnode.TextValue == v);
        }

        #endregion

        public static void Run()
        {
            Console.WriteLine(nameof(TestJsonModelBuilder));
            var o = new TestJsonModelBuilder();
            o.testPrimitive();
            o.testClass();
            o.testArray();
            o.testEnum();
            o.testList();
            o.testDictionary();
        }
    }
}
