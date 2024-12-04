using System;
using System.Collections.Generic;
using System.Text;
using Nano.Json;
using Nano.Nuts;
using Nano.UnitTest;

namespace TestExt.TestNuts
{
	class TestObject
	{
        static void testValues()
        {
            DObject o = 123;
            long vl = o;
            Test.Assert(o.IsInt && o.NodeType == JsonNodeType.Integer);
            Test.Assert(vl == 123 && o == 123 && (int)o == 123);

            o = DObject.New((int)2333);
            Test.Assert(o == 2333);

            o = DObject.New((uint)2333);
            Test.Assert(o == 2333);

            o = DObject.New((ulong)2333);
            Test.Assert(o == 2333);

            o = 3.14;
            double vd = o;
            Test.Assert(o.IsFloat && o.NodeType == JsonNodeType.Float);
            Test.Assert(vd == 3.14 && o == 3.14);

            o = DObject.New((float)3.14);
            Test.Assert(Math.Abs(o - 3.14) < 2e-7);

            o = "Hello";
            string vs = o;
            Test.Assert(o.IsString && o.NodeType == JsonNodeType.String);
            Test.Assert(vs == "Hello" && o == "Hello");

            o = true;
            bool vb = o;
            Test.Assert(o.IsBool && o.NodeType == JsonNodeType.Boolean);
            Test.Assert(vb && o);
            if (!o)
                Test.Fail();

            o = false;
            vb = o;
            Test.Assert(o.IsBool && o.NodeType == JsonNodeType.Boolean);
            Test.Assert(!vb && !o);
            if (o)
                Test.Fail();

            o = DObject.Null;
            Test.Assert(o.IsNull && o.NodeType == JsonNodeType.Null);
        }

        static void testValues2()
		{
            var o = DObject.New(123);
			long vl = o;
            Test.Assert(vl == 123 && o == 123 && o == 123L);

			o = DObject.New(3.14);
			double vd = o;
            Test.Assert(vd == 3.14 && o == 3.14);

			o = DObject.New("Hello");
			string vs = o;
            Test.Assert(vs == "Hello" && o == "Hello");

			o = DObject.New(true);
			bool vb = o;
            Test.Assert(vb && o);

			o = DObject.New(false);
			vb = o;
            Test.Assert(!vb && !o);

			o = DObject.New(null);
            Test.Assert(o.IsNull);
		}

		static void testInitList()
		{
			var o = DObject.New(new DObject.DList { "Hello", 123, 3.14, true, null });
            Test.Assert(o.IsList && o.NodeType == JsonNodeType.NodeList);
            Test.Assert(o.Count == 5);
            Test.Assert(o[1] == 123 && o[3]);

            o = (DObject)new DObject.DList { "Hello", 123, 3.14, true, null };
            Test.Assert(o.Count == 5);
            Test.Assert(o[1] == 123 && o[3]);

            o = (DObject)new DObject.DList
            {
                new DObject.DList { 1, 2 },
                new [] { 3, 4 }
            };
            Test.Assert(o.Count == 2);
            var oi = o[0];
            Test.Assert(oi.Count == 2 && oi[0] == 1 && oi[1] == 2);
            oi = o[1];
            Test.Assert(oi.Count == 2 && oi[0] == 3 && oi[1] == 4);

            o = DObject.New(new object[] { "Hello", 123, 3.14, true, null });
            Test.Assert(o.Count == 5);
            Test.Assert(o[1] == 123 && o[3]);

            o = DObject.New(new List<object> { "Hello", 123, 3.14, true, null });
            Test.Assert(o.Count == 5);
            Test.Assert(o[1] == 123 && o[3]);

            o = (DObject)new object[] { "Hello", 123, 3.14, true, null };
            Test.Assert(o.Count == 5);
            Test.Assert(o[1] == 123 && o[3]);
        }

        class User
		{
			public string name;
            public int stat;
		}

		static void testInitMap()
		{
			var o = DObject.New(new DObject.DMap { { "red", 1 }, { "orange", "closed" } });
            Test.Assert(o.IsMap && o.NodeType == JsonNodeType.Dictionary);
            Test.Assert(o.Count == 2);
            Test.Assert(o["red"] == 1 && o["orange"] == "closed");
            // Test.Assert(o.red == 1 && o.orange == "closed"); // not supported

            o = (DObject)new DObject.DMap { { "red", 1 }, { "orange", "closed" } };
            Test.Assert(o.Count == 2);
            Test.Assert(o["red"] == 1 && o["orange"] == "closed");

            o = DObject.New(new Dictionary<string, object> { { "red", 1 }, { "orange", "closed" } });
            Test.Assert(o.Count == 2);
            Test.Assert(o["red"] == 1 && o["orange"] == "closed");

            o = DObject.New(new { red = 1, orange = "closed" });
            Test.Assert(o.Count == 2);
            Test.Assert(o["red"] == 1 && o["orange"] == "closed");

            o = DObject.New(new User { name = "Zhang", stat = 1 });
            Test.Assert(o.Count == 2);
            Test.Assert(o["stat"] == 1 && o["name"] == "Zhang");

            o = new DObject.DMap
            {
                { "stat", "ok" },
                { "user", new { name = "Louis" } }
            };
            Test.Assert(o.Count == 2);
            Test.Assert(o["stat"] == "ok" && o["user"].Count == 1 && o["user"]["name"] == "Louis");

            o = DObject.New(new DObject.DMap
			{
				{ "result", "ok" },
				{ "items", new DObject.DList
					{
						new DObject.DMap { { "name", "red" }, { "value", 1 } },
						new DObject.DMap { { "name", "orange" }, { "value", 2 } }
					}
				}
			});
            Test.Assert(o.Count == 2 && o["result"] == "ok");
			var oi = o["items"][1];
            Test.Assert(oi["name"] == "orange" && oi["value"] == 2);            
		}

		static void testTransform()
		{
			var o = DObject.Transform(new int[] { 2, 5, 8 }, x => new DObject.DMap
			{
				{ "name", x.ToString() }
			});
            Test.Assert(o.IsList && o.Count == 3 && o[1]["name"] == "5");

            var Zhang = new User { name = "Zhang", stat = 0 };
            var Zhao = new User { name = "Zhao", stat = 1 };
            var Wang = new User { name = "Wang", stat = 0 };
            var users = new[] { Zhang, Zhao, Wang };
            o = DObject.Transform(users, x => x.name, x => x.stat == 0);
            Test.Assert(o.IsList && o.Count == 2 && o[0] == "Zhang" && o[1] == "Wang");

            var usermap = new Dictionary<string, User> { { Zhang.name, Zhang }, { Zhao.name, Zhao }, { Wang.name, Wang } };
            o = DObject.TransformMap(usermap, x => x.name, x => x.stat == 0);
            Test.Assert(o.IsMap && o.Count == 2 && o["Zhang"] == "Zhang" && o["Wang"] == "Wang");
        }

		static void testJson()
		{
			var s = "{\"r\":\"ok\",\"items\":[{\"name\":\"red\"},{\"name\":\"green\"}]}";
			var jnode = JsonParser.ParseText(s);
			var o = DObject.New(jnode);
            Test.Assert(o["r"] == "ok");
			var oitems = o["items"];
            Test.Assert(oitems.Count == 2 && oitems[1]["name"] == "green");

			jnode = DObject.ExportJson(o);
            Test.Assert(jnode.NodeType == JsonNodeType.Dictionary && jnode["r"].TextValue == "ok");
			jnode = jnode["items"];
            Test.Assert(jnode.NodeType == JsonNodeType.NodeList && jnode.ChildCount == 2 && jnode[1]["name"].TextValue == "green");

			string st = DObject.ExportJsonStr(o);
            Test.Assert(st == s);

			o = DObject.New(new DObject.DMap {
				{ "name", "test" }, { "props", null }
			});
			st = DObject.ExportJsonStr(o);
            Test.Assert(st == "{\"name\":\"test\",\"props\":null}");
		}

		static void testToString()
		{
			DObject o = DObject.New(123);
            Test.Assert(o.ToString() == "123");

			o = DObject.New(-3.14);
            Test.Assert(o.ToString() == "-3.14");

			o = DObject.New(true);
            Test.Assert(o.ToString() == "true");
			o = DObject.New(false);
            Test.Assert(o.ToString() == "false");

			o = DObject.New("I say \"Hello\"");
            Test.Assert(o.ToString() == "\"I say \\\"Hello\\\"\"");

			o = DObject.New(null);
            Test.Assert(o.ToString() == "null");

			o = DObject.New(new DObject.DList { 1, 2, 3 });
            Test.Assert(o.ToString() == "[1,2,3]");

			o = DObject.New(new DObject.DMap { { "a", 1 }, { "b", "2" } });
            Test.Assert(o.ToString() == "{\"a\":1,\"b\":\"2\"}");

			o = DObject.New(new DObject.DList { 1, 2, 3 });
			o = DObject.New(new DObject.DMap { { "stat", "ok" }, { "ids", o } });
            Test.Assert(o.ToString() == "{\"stat\":\"ok\",\"ids\":[1,2,3]}");
		}

		public static void Run()
		{
			Console.WriteLine("TestNuts.TestObject");
            testValues();
			testValues2();
			testInitList();
			testInitMap();
			testTransform();
			testJson();
			testToString();
		}
	}
}
