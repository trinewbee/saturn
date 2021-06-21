using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;

namespace TestCommon.TestJson
{
    class Test_JsonFormat
    {
		static void assertStringEqual(string x, string y) => Test.Assert(string.CompareOrdinal(x, y) == 0);

        static void TestFormatValue()
        {
            JNode v = JNode.Undefined;
			Test.AssertExceptionType<TypeException>(() => JNode.Stringify(v));

            v = JNode.Null;
            assertStringEqual("null", JNode.Stringify(v));

            v = true;
            assertStringEqual("true", JNode.Stringify(v));

            v = false;
            assertStringEqual("false", JNode.Stringify(v));

            v = 1;
            assertStringEqual("1", JNode.Stringify(v));

            v = 1L;
            assertStringEqual("1", JNode.Stringify(v));

            v = 'c';
            assertStringEqual("99", JNode.Stringify(v));

            v = 1.0f;
            assertStringEqual("1", JNode.Stringify(v));
            
            v = 1.0;
            assertStringEqual("1", JNode.Stringify(v));
        }

        static void TestFormatString()
        {
            JNode v = "";
            assertStringEqual("\"\"", JNode.Stringify(v));

            v = " ";
            assertStringEqual("\" \"", JNode.Stringify(v));

            v = @"c:\b";
            var s = JNode.Stringify(v);
            assertStringEqual("\"c:\\\\b\"", s);

            JNode v2 = JNode.Parse(s);
            assertStringEqual(@"c:\b", v.TextValue);
        }

        static void TestFormatArray()
        {
            JNode v = new object[] {};
            assertStringEqual("[]", JNode.Stringify(v));

            v = new object[] { 1, "", 'c', true, null };
            assertStringEqual("[1,\"\",99,true,null]", JNode.Stringify(v));
        }

        static void TestFormatObject()
        {
            JNode v = JNode.Create(new {});
            assertStringEqual("{}", JNode.Stringify(v));

            v = JNode.Create(new { a = 1, b = "", c = 'c', d = true, e = (object)null });
            string s = JNode.Stringify(v);
            // https://docs.microsoft.com/zh-cn/dotnet/standard/frameworks
#if NET48
            Test.Assert(s == "{\"e\":null,\"d\":true,\"a\":1,\"c\":99,\"b\":\"\"}");
#else
            _checkJsonMap(s, "\"a\":1", "\"b\":\"\"", "\"c\":99", "\"d\":true", "\"e\":null");
#endif
        }

        static void _checkJsonMap(string s, params string[] parts)
        {
            // NETCORE 下，枚举器生成的结果是乱序
            Test.Assert(s[0] == '{' && s[s.Length - 1] == '}');
            s = s.Substring(1, s.Length - 2);
            var ss = s.Split(',');
            Test.Assert(ss.Length == parts.Length);
            Array.Sort(ss);
            for (int i = 0; i < ss.Length; ++i)
                Test.Assert(ss[i] == parts[i]);
        }

        public static void Run()
        {
            Console.WriteLine("Tests.JSON.Net.Test_JsonFormat");
            TestFormatValue();
            TestFormatString();
            TestFormatArray();
            TestFormatObject();
        }
    }
}
