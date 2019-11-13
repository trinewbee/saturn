using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nano.Json;
using Nano.Nuts;
using Puff.Marshal;

namespace Puff.Model
{
    class ApiInvoker
    {
        MethodInBuilder m_mib;
        // MethodOutBuilder m_mob;
        MethodOutBuilder2 m_mob2;

        public ApiInvoker(JsonObjectBuilder job, JsonModelBuilder jmb)
        {
            m_mib = new MethodInBuilder(job);
            // m_mob = new MethodOutBuilder();
            m_mob2 = new MethodOutBuilder2(jmb);
        }

        public object[] PrepareJsonMethodArgs(MethodInfo m, JsonNode body, Dictionary<string, string> cookies) => m_mib.PrepareJsonMethodArgs(m, body, cookies);

        public object Invoke(JmMethod m, object[] args) => m.MI.Invoke(m.Instance, args);

        // public JsonNode BuildMethodReturn(JmMethod m, object ret) => m_mob.BuildMethodReturn(m, ret);

        public IceApiResponse BuildJsonStyleApiReturn(JmMethod m, object ret) => m_mob2.BuildJsonStyleApiReturn(m, ret);

        public JsonNode BuildNotifyArgs(JmNotify notify, object[] args)
		{
            /*
			var prms = notify.MI.GetParameters();
			G.Verify(prms.Length == args.Length + 1, "InvalidArgNum");

			var jn = new JsonNode(JsonNodeType.Dictionary);
			var jni = new JsonNode(notify.Url) { Name = "sc:m" };
			jn.AddChildItem(jni);

			for (int i = 1; i < prms.Length; ++i)
			{
				jni = m_mob.BuildObject(args[i - 1]);
				jni.Name = prms[i].Name;
				jn.AddChildItem(jni);
			}
			return jn;
            */
            return m_mob2.BuildNotifyArgs(notify, args);

        }
	}
}
