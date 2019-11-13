using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
    class Test_JsonBuild
    {
        static void testBuildConst()
        {
            JNode root = default(JNode);
            Test.Assert(root.IsUndefined);
            Test.Assert(root == JNode.Undefined);
			Test.Assert(!root.IsNull);
            Test.Assert(!root.IsBoolean);
			Test.Assert(!root.IsInteger);
            Test.Assert(!root.IsFloat);
			Test.Assert(!root.IsString);
            Test.Assert(!root.IsObject);
			Test.Assert(!root.IsArray);

            root = JNode.Undefined;
            Test.Assert(root.IsUndefined);
            Test.Assert(root == default(JNode));

            root = JNode.Null;
            Test.Assert(root.IsNull);
            Test.Assert(root.IsText);
            Test.Assert(root.IsNullable);

            root = JNode.True;
            Test.Assert(root.IsBoolean);
            Test.Assert(root.BoolValue == true && (bool)root == true);

            root = JNode.False;
            Test.Assert(root.IsBoolean);
            Test.Assert(root.BoolValue == false && (bool)root == false);

        }

        static void testBuildBoolean()
        {
            bool bv = true;
            JNode root = JNode.Create(bv);
            JNode node = bv;
            JNode value = new JNode(bv);
            Test.Assert(root.IsBoolean && root.BoolValue == bv && (bool)root == bv);
            Test.Assert(node.IsBoolean && node.BoolValue == bv && (bool)node == bv);
            Test.Assert(value.IsBoolean && value.BoolValue == bv && (bool)value == bv);

            bv = false;
            root = JNode.Create(bv);
            node = bv;
            value = new JNode(bv);
            Test.Assert(root.IsBoolean && root.BoolValue == bv && (bool)root == bv);
            Test.Assert(node.IsBoolean && node.BoolValue == bv && (bool)node == bv);
            Test.Assert(value.IsBoolean && value.BoolValue == bv && (bool)value == bv);
        }

        static void testBuildInteger()
        {
            byte bv = (byte)10;
            JNode root = JNode.Create(bv);
            JNode node = bv;
            JNode value = new JNode(bv);
            Test.Assert(root.IsInteger && root.IntValue == (long)bv && (byte)root == bv);
            Test.Assert(node.IsInteger && node.IntValue == (long)bv && (byte)node == bv);
            Test.Assert(value.IsInteger && value.IntValue == (long)bv && (byte)value == bv);

            sbyte sb = (sbyte)10;
            root = JNode.Create(sb);
            node = sb;
            value = new JNode(sb);
            Test.Assert(root.IsInteger && root.IntValue == (long)sb && (sbyte)root == sb);
            Test.Assert(node.IsInteger && node.IntValue == (long)sb && (sbyte)node == sb);
            Test.Assert(value.IsInteger && value.IntValue == (long)sb && (sbyte)value == sb);

            short sv = (short)10;
            root = JNode.Create(sv);
            node = sv;
            value = new JNode(sv);
            Test.Assert(root.IsInteger && root.IntValue == (long)sv && (short)root == sv);
            Test.Assert(node.IsInteger && node.IntValue == (long)sv && (short)node == sv);
            Test.Assert(value.IsInteger && value.IntValue == (long)sv && (short)value == sv);

            ushort us = (ushort)10;
            root = JNode.Create(us);
            node = us;
            value = new JNode(us);
            Test.Assert(root.IsInteger && root.IntValue == (long)us && (ushort)root == us);
            Test.Assert(node.IsInteger && node.IntValue == (long)us && (ushort)node == us);
            Test.Assert(value.IsInteger && value.IntValue == (long)us && (ushort)value == us);


            int nv = 1;
            root = JNode.Create(nv);
            node = nv;
            value = new JNode(nv);
            Test.Assert(root.IsInteger && root.IntValue == nv && (long)root == nv && (int)root == nv);
            Test.Assert(node.IsInteger && node.IntValue == nv && (long)node == nv && (int)node == nv);
            Test.Assert(value.IsInteger && value.IntValue == nv && (long)value == nv && (int)value == nv);           


            uint un = 1;
            root = JNode.Create(un);
            node = un;
            value = new JNode(un);
            Test.Assert(root.IsInteger && root.IntValue == (long)un && (uint)root == un);
            Test.Assert(node.IsInteger && node.IntValue == (long)un && (uint)node == un);
            Test.Assert(value.IsInteger && value.IntValue == (long)un && (uint)value == un);

            long lv = -1L;
            root = JNode.Create(lv);
            node = lv;
            value = new JNode(lv);
            Test.Assert(root.IsInteger && root.IntValue == lv && (long)root == lv && (long)root == lv);
            Test.Assert(node.IsInteger && node.IntValue == lv && (long)node == lv && (long)node == lv);
            Test.Assert(value.IsInteger && value.IntValue == lv && (long)value == lv && (long)value == lv);

            ulong ul = 1;
            root = JNode.Create(ul);
            node = ul;
            value = new JNode(ul);
            Test.Assert(root.IsInteger && root.IntValue == (long)ul && (ulong)root == ul);
            Test.Assert(node.IsInteger && node.IntValue == (long)ul && (ulong)node == ul);
            Test.Assert(value.IsInteger && value.IntValue == (long)ul && (ulong)value == ul);
        }

        static void testBuildFloat()
        {
            float fv = (byte)10;
            JNode root = JNode.Create(fv);
            JNode node = fv;
            JNode value = new JNode(fv);
            Test.Assert(root.IsFloat && root.FloatValue == (double)fv && (float)root == fv);
            Test.Assert(node.IsFloat && node.FloatValue == (double)fv && (float)node == fv);
            Test.Assert(value.IsFloat && value.FloatValue == (double)fv && (float)value == fv);

            double dv = (byte)10;
            root = JNode.Create(dv);
            node = dv;
            value = new JNode(dv);
            Test.Assert(root.IsFloat && root.FloatValue == (double)dv && (double)root == dv);
            Test.Assert(node.IsFloat && node.FloatValue == (double)dv && (double)node == dv);
            Test.Assert(value.IsFloat && value.FloatValue == (double)dv && (double)value == dv);
        }

        static void testBuildString()
        {
            string sv = "1111";
            JNode root = JNode.Create(sv);
            JNode node = sv;
            JNode value = new JNode(sv);
            Test.Assert(root.IsNullable);
            Test.Assert(root.IsString && root.IsText && root.TextValue == sv && (string)root == sv);
            Test.Assert(node.IsString && node.IsText && node.TextValue == sv && (string)node == sv);
            Test.Assert(value.IsString && value.IsText && value.TextValue == sv && (string)value == sv);

            sv = "";
            root = JNode.Create(sv);
            node = sv;
            value = new JNode(sv);
            Test.Assert(root.IsString && root.IsText && root.TextValue == sv && (string)root == sv);
            Test.Assert(node.IsString && node.IsText && node.TextValue == sv && (string)node == sv);
            Test.Assert(value.IsString && value.IsText && value.TextValue == sv && (string)value == sv);

            sv = null;
            root = JNode.Create(sv);
            node = sv;
            value = new JNode(sv);
			Test.Assert(!root.IsString);
            Test.Assert(root.IsText && root.TextValue == sv && (string)root == sv);
            Test.Assert(node.IsString && node.IsText && node.TextValue == sv && (string)node == sv);
            Test.Assert(value.IsString && value.IsText && value.TextValue == sv && (string)value == sv);

            object ov = null;
            root = JNode.Create(ov);
            node = JNode.Null;
			Test.Assert(!root.IsString);
            Test.Assert(root.IsText && root.TextValue == (string)ov && (string)root == (string)ov);
			Test.Assert(!node.IsString);
            Test.Assert(node.IsText && node.TextValue == (string)ov && (string)node == (string)ov);
        }

        static void TestBuildArray()
        {
            JNode root = JNode.NewArray();
            Test.Assert(root.IsArray && root.Length == 0);

            root = JNode.NewArray(new object[] { 1, "2" });
            Test.Assert(root.IsArray && root.Length == 2);
            Test.Assert(root[0].IsInteger && root[0] == 1);
            Test.Assert(root[1].IsString && root[1] == "2");

            root = JNode.NewArray(new List<object>() { 1, "2" });
            Test.Assert(root.IsArray && root.Length == 2);
            Test.Assert(root[0].IsInteger && root[0] == 1);
            Test.Assert(root[1].IsString && root[1] == "2");

            root = JNode.NewArray(1, "2", true, false, 1.0);
            Test.Assert(root.IsArray && root.Length == 5);
            Test.Assert(root[0].IsInteger && root[0] == 1);
            Test.Assert(root[1].IsString && root[1] == "2");
            Test.Assert(root[2].IsBoolean && root[2] == true);
            Test.Assert(root[3].IsBoolean && root[3] == false);
            Test.Assert(root[4].IsFloat && root[4] == 1.0);

            root = JNode.Create(new object[] {
                true, false,
                (byte)1, (sbyte)2,
                (short)1, (ushort)2,
                (int)1, (uint)2,
                (long)1L, (ulong)2L,
                (float)1.11f, (double)2.22,
                (string)"adb", (string)null,
                (object)"adb", (object)null,
                JNode.Null, JNode.True, JNode.False, new JNode(1.0f),
                new JNode("bbb"),
                JNode.NewArray(),
                JNode.NewObject(),
                new object[] { "a" }
            });

            Test.Assert(root.IsArray && root.Length == 24);
            Test.Assert(root[0].IsBoolean && root[0] == true);
            Test.Assert(root[1].IsBoolean && root[1] == false);

            Test.Assert(root[2].IsInteger && root[2] == ((byte)1));
            Test.Assert(root[3].IsInteger && root[3] == (sbyte)2);

            Test.Assert(root[4].IsInteger && root[4] == (short)1);
            Test.Assert(root[5].IsInteger && root[5] == (ushort)2);

            Test.Assert(root[6].IsInteger && root[6] == (int)1);
            Test.Assert(root[7].IsInteger && root[7] == (uint)2);

            Test.Assert(root[8].IsInteger && root[8] == (long)1);
            Test.Assert(root[9].IsInteger && root[9] == (ulong)2);

            Test.Assert(root[10].IsFloat && root[10] == (float)1.11f);
            Test.Assert(root[11].IsFloat && root[11] == (double)2.22);

            Test.Assert(root[12].IsString && root[12] == "adb");
            Test.Assert(root[13].IsNull && !root[13].IsString && root[13] == (string)null);

            Test.Assert(root[14].IsString && root[14] == "adb");
            Test.Assert(root[15].IsNull && !root[15].IsString);

            Test.Assert(root[16].IsNull);

            Test.Assert(root[17].IsBoolean && root[17] == true);
            Test.Assert(root[18].IsBoolean && root[18] == false);
            Test.Assert(root[19].IsFloat && root[19] == 1.0);

            Test.Assert(root[20].IsString && root[20] == "bbb");
            Test.Assert(root[21].IsArray && root[21].Length == 0);
            Test.Assert(root[22].IsObject && root[22].Count == 0);

            Test.Assert(root[23].IsArray && root[23].Length == 1);
            Test.Assert(root[23][0].IsString && root[23][0] == "a");

            root = new object[] { 1 };
            Test.Assert(root.IsArray);
            Test.Assert(root.Length == 1);
            Test.Assert(root[0] == 1);

            Test.AssertExceptionType<TypeException>(() => root = new object[] { JNode.Undefined } );
        }

        static void TestBuildObject()
        {
            JNode root = JNode.NewObject();
            Test.Assert(root.IsObject && root.Count == 0);

            root = JNode.NewObject(new Dictionary<string, object>() { { "a", 1} , { "b", true } });
            Test.Assert(root.IsObject && root.Count == 2);
            Test.Assert(root["a"] == 1 && root["b"] == true);

            root = JNode.NewObject(new ConcurrentDictionary<string, object>());
            Test.Assert(root.IsObject && root.Count == 0);

            root = JNode.Create(new Dictionary<string, object>() { { "a", 1 }, { "b", true } });
            Test.Assert(root.IsObject && root.Count == 2);
            Test.Assert(root["a"] == 1 && root["b"] == true);

            root = JNode.Create(new ConcurrentDictionary<string, object>());
            Test.Assert(root.IsObject && root.Count == 0);

            root = JNode.Create(new { a=1, 键=1.2 });
            Test.Assert(root.IsObject && root.Count == 2);
            Test.Assert(root["a"] == 1 && root["键"] == 1.2);

			Test.AssertExceptionType<TypeException>(() => root = JNode.Create(new { u = JNode.Undefined }));
        }

        public static void Run()
        {
            Console.WriteLine("Tests.JNode.Net.Test_JNodeBuild");
            testBuildConst();
            testBuildBoolean();
            testBuildInteger();
            testBuildFloat();
            testBuildString();
            TestBuildArray();
            TestBuildObject();
        }
    }
}
