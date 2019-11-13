using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.UnitTest;
using Nano.Ext.Marshal;
using Puff.Marshal;

namespace Puff.Model
{
	class TestMethodInBuilder
    {
        MethodInBuilder mib;

        TestMethodInBuilder()
        {
            var job = JsonObjectBuilder.BuildDefault();
            mib = new MethodInBuilder(job);
        }

        void testMethodArgs()
		{
			object o = new Dictionary<string, object> { { "t", "abc" }, { "x", 123 } };
			var jn = JsonModel.Dump(o);
			var m = typeof(JMT_X).GetMethod("f");
            var items = mib.PrepareJsonMethodArgs(m, jn, null);
			Test.Assert(items.Length == 2 && (string)items[0] == "abc" && (int)items[1] == 123);
		}

		public static void Run()
		{
			Console.WriteLine(nameof(TestMethodInBuilder));
			var o = new TestMethodInBuilder();
			o.testMethodArgs();
		}
	}
}
