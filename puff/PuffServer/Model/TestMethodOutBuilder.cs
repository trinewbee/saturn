using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;
using Nano.Ext.Marshal;
using Nano.Nuts;
using Puff.Marshal;

namespace Puff.Model
{
	class TestMethodOutBuilder
	{
        // MethodOutBuilder jmb = new MethodOutBuilder();
        MethodOutBuilder2 mob = new MethodOutBuilder2(JsonModelBuilder.BuildDefault());

        #region Old version

        /*
        // 常规返回值
        void testR2Return()
		{
            // 无返回值
			var jn = jmb.BuildReturn("ok", new string[0], null);
			Test.Assert(jn.ChildCount == 1 && jn["stat"] == "ok");

            // 单一返回值，Ret 指定名字
			var o = new Tuple<string, int>("abc", 123);
			jn = jmb.BuildReturn("ok", new string[] { "x" }, o);
			Test.Assert(jn.ChildCount == 2 && jn["stat"] == "ok");
			jn = jn["x"];
			Test.Assert(jn.ChildCount == 2 && jn["Item1"].TextValue == "abc" && jn["Item2"].IntValue == 123);

            // 多个返回值，Ret 指定名字
			jn = jmb.BuildReturn("ok", new string[] { "t", "x" }, o);
			Test.Assert(jn.ChildCount == 3 && jn["stat"] == "ok" && jn["t"].TextValue == "abc" && jn["x"].IntValue == 123);
		}

        // DObject 返回值
		void testR2DObject()
		{
			DObject o = new DObject.DMap();
			Test.AssertException(() => jmb.BuildReturn("ok", new string[0], o));

			o = new DObject.DMap { { "name", "apple" } };
			var jn = jmb.BuildReturn("ok", new string[] { "item" }, o);
			Test.Assert(jn.ChildCount == 2 && jn["stat"] == "ok" && jn["item"]["name"] == "apple");

			o = new DObject.DMap { { "name", "apple" }, { "value", 123 } };
			jn = jmb.BuildReturn("ok", new string[] { "name", "value" }, o);
			Test.Assert(jn.ChildCount == 3 && jn["stat"] == "ok" && jn["name"] == "apple" && jn["value"] == 123);

			o = new DObject.DList {
				new DObject.DMap { { "name", "red" }, { "value", 1 } },
				new DObject.DMap { { "name", "green" }, { "value", 2 } },
			};
			jn = jmb.BuildReturn("ok", new string[] { "items" }, o);
			jn = jn["items"];
			Test.Assert(jn.NodeType == JsonNodeType.NodeList && jn.ChildCount == 2);
			Test.Assert(jn[0]["name"] == "red" && jn[1]["value"] == 2);

			o = new DObject.DMap { { "count", 2 }, { "items", o } };
			jn = jmb.BuildReturn("ok", new string[] { "count", "items" }, o);
			Test.Assert(jn["count"] == 2 && jn["items"][1]["name"] == "green");

            o = DObject.Transform(new int[] { 2, 5, 8 }, x => DObject.New(new { id = x, value = x.ToString() }));
			jn = jmb.BuildReturn("ok", new string[] { "items" }, o);
			Test.Assert(jn["items"][1]["id"] == 5);
		}
        */

        #endregion

        #region Json Style Return

        class My
        {
            public string x;
            public int y;
        }

        void testJSR_Single()
        {
            var jn = mob.BuildJSR_Main(null);
            _assert_map(jn, 0);

            jn = mob.BuildJSR_Main(123, "x");
            _assert_map(jn, 1);
            Test.Assert(jn["x"] == 123);

            jn = mob.BuildJSR_Main(new object[] { "test" }, "x");
            _assert_map(jn, 1);
            var jni = jn["x"];
            _assert_list(jni, 1);
            Test.Assert(jni[0] == "test");

            jn = new DObject.DMap { { "x", "test" } }.ToJson();
            jn = mob.BuildJSR_Main(jn, "x");
            _assert_map(jn, 1);
            jni = jn["x"];
            _assert_map(jni, 1);
            Test.Assert(jni["x"] == "test");

            DObject d = new DObject.DMap { { "x", "test" } };
            jn = mob.BuildJSR_Main(d, "x");
            _assert_map(jn, 1);
            jni = jn["x"];
            _assert_map(jni, 1);
            Test.Assert(jni["x"] == "test");

            object o = new Dictionary<string, object> { { "x", "test" } };
            jn = mob.BuildJSR_Main(o, "x");
            _assert_map(jn, 1);
            jni = jn["x"];
            _assert_map(jni, 1);
            Test.Assert(jni["x"] == "test");

            jn = mob.BuildJSR_Main(new My { x = "test", y = 123 }, "x");
            _assert_map(jn, 1);
            jni = jn["x"];
            _assert_map(jni, 2);
            Test.Assert(jni["x"] == "test" && jni["y"] == 123);

            jn = mob.BuildJSR_Main(new { x = "test" }, "x");
            _assert_map(jn, 1);
            jni = jn["x"];
            _assert_map(jni, 1);
            Test.Assert(jni["x"] == "test");
        }

        void testJSR_NoRet()
        {
            var jn = new DObject.DMap { { "x", "test" }, { "y", 123 } }.ToJson();
            jn = mob.BuildJSR_Main(jn);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            DObject d = new DObject.DMap { { "x", "test" }, { "y", 123 } };
            jn = mob.BuildJSR_Main(d);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            object o = new Dictionary<string, object> { { "x", "test" }, { "y", 123 } };
            jn = mob.BuildJSR_Main(o);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            jn = mob.BuildJSR_Main(new My { x = "test", y = 123 });
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            jn = mob.BuildJSR_Main(new { x = "test", y = 123 });
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);
        }

        void testJSR_RetList()
        {
            var names = new string[] { "x", "y" };
            var jn = mob.BuildJSR_Main(new Tuple<string, int>("test", 123), names);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            jn = mob.BuildJSR_Main(("test", 123), names);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            jn = mob.BuildJSR_Main(new object[] { "test", 123 }, names);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);

            jn = mob.BuildJSR_Main(new List<object> { "test", 123 }, names);
            _assert_map(jn, 2);
            Test.Assert(jn["x"] == "test" && jn["y"] == 123);
        }
        
        void testJSR_Stat()
        {
            var jn = new DObject.DMap { { "x", 123 } }.ToJson();
            MethodOutBuilder2.BuildJSR_AddStat(jn, "stat", "ok");
            _assert_map(jn, 2);
            Test.Assert(jn["stat"] == "ok" && jn["x"] == 123);

            jn = new DObject.DMap { { "x", 123 }, { "stat", "false" } }.ToJson();
            MethodOutBuilder2.BuildJSR_AddStat(jn, "stat", "ok");
            _assert_map(jn, 2);
            Test.Assert(jn["stat"] == "false" && jn["x"] == 123);

            jn = new DObject.DMap { { "x", 123 } }.ToJson();
            MethodOutBuilder2.BuildJSR_AddStat(jn, "", "ok");
            _assert_map(jn, 1);
            Test.Assert(jn["x"] == 123);

            var m = new JmMethod { StatKey = "Stat" };
            jn = mob.BuildJsonStyleReturn(m, new { x = 123 });
            _assert_map(jn, 2);
            Test.Assert(jn["Stat"] == "ok" && jn["x"] == 123);

            jn = mob.BuildJsonStyleReturn(m, new { Stat = "false", x = 123 });
            _assert_map(jn, 2);
            Test.Assert(jn["Stat"] == "false" && jn["x"] == 123);

            m.StatKey = null;
            jn = mob.BuildJsonStyleReturn(m, new { x = 123 });
            _assert_map(jn, 1);
            Test.Assert(jn["x"] == 123);

            m.StatKey = "";
            jn = mob.BuildJsonStyleReturn(m, new { x = 123 });
            _assert_map(jn, 1);
            Test.Assert(jn["x"] == 123);
        }

        void testJSR_Cookies()
        {
            var jn = new DObject.DMap { { "x", "test" }, { "y", 123 } }.ToJson();
            var map = MethodOutBuilder2.BuildJSR_Cookies(jn);
            Test.Assert(map == null);

            map = MethodOutBuilder2.BuildJSR_Cookies(jn, (string[])null);
            Test.Assert(map == null);

            map = MethodOutBuilder2.BuildJSR_Cookies(jn, "x");
            Test.Assert(map.Count == 1 && map["x"] == "test");

            map = MethodOutBuilder2.BuildJSR_Cookies(jn, "x", "y");
            Test.Assert(map.Count == 2 && map["x"] == "test" && map["y"] == "123");

            var m = new JmMethod { };
            var r = mob.BuildJsonStyleApiReturn(m, new { x = "test", y = 123 });
            Test.Assert(r.HttpStatusCode == 200 && r.Cookies == null);

            m.Cookies = new string[] { "x" };
            r = mob.BuildJsonStyleApiReturn(m, new { x = "test", y = 123 });
            Test.Assert(r.HttpStatusCode == 200 && r.Cookies.Count == 1 && r.Cookies["x"] == "test");

            m.Cookies = new string[] { "x", "y" };
            r = mob.BuildJsonStyleApiReturn(m, new { x = "test", y = 123 });
            Test.Assert(r.HttpStatusCode == 200 && r.Cookies.Count == 2 && r.Cookies["x"] == "test" && r.Cookies["y"] == "123");
        }

        #endregion

        static void _assert_list(JsonNode jnode, int count) =>
            Test.Assert(jnode.NodeType == JsonNodeType.NodeList && jnode.ChildCount == count);

        static void _assert_map(JsonNode jnode, int count) => 
            Test.Assert(jnode.NodeType == JsonNodeType.Dictionary && jnode.ChildCount == count);

        public static void Run()
		{
			Console.WriteLine(nameof(TestMethodOutBuilder));
			var o = new TestMethodOutBuilder();

			// o.testR2Return();
			// o.testR2DObject();

            o.testJSR_Single();
            o.testJSR_NoRet();
            o.testJSR_RetList();
            o.testJSR_Stat();
            o.testJSR_Cookies();
        }
	}
}
