using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Nano.Json;
using Nano.Sockets;
using Nano.Nuts;
using Puff.Model;

namespace Puff.Servers
{
	class SocketsModule
	{
		JmModule m_mod;
        ApiInvoker m_ivk;

		public SocketsModule(JmModule mod, ApiInvoker ivk)
		{
			m_mod = mod;
            m_ivk = ivk;
		}

		public void LinkNotify(object oTag, MethodInfo miTag)
		{
			foreach (var notify in m_mod.Notifies.Values)
				notify.LinkNotify(oTag, miTag);
		}

		public JsonNode Invoke(string url, JsonNode jnode)
		{
			try
			{
				JmMethod m = m_mod.GetMethod(url);

				if (m == null)
				{
					Console.WriteLine("MethodNotFound, Url=" + url);
					// Logger.errLog.Write("MethodNotFound", "MethodNotFound, Url=" + url);
					// WebGlobal.curEnv.stat = "MethodNotFound";
					return ErrorStat("MethodNotFound", "接口没有找到");
				}
				else if (m.Flags == IceApiFlag.Json)
				{
                    var args = m_ivk.PrepareJsonMethodArgs(m.MI, jnode, null);
                    var ret = m_ivk.Invoke(m, args);
                    // jnode = m_ivk.BuildMethodReturn(m, ret);
                    var r = m_ivk.BuildJsonStyleApiReturn(m, ret);
                    G.Verify(r.Json != null, "NotJsonStyle");
                    return r.Json;
				}
				else
				{
					Console.WriteLine("UnsupportedMethodStyle, Url=" + url);
					// Logger.errLog.Write("UnsupportedMethodStyle", "UnsupportedMethodStyle, Url=" + url);
					// WebGlobal.curEnv.stat = "UnsupportedMethodStyle";
					return ErrorStat("UnsupportedMethodStyle", "不支持的接口风格");
				}
			}
			catch (System.Reflection.TargetInvocationException e)
			{
				NutsException inner = e.InnerException as NutsException;
				if (inner != null)
				{
					Console.WriteLine("NutsException: " + inner.Code);
					// Logger.errLog.Write("NutsException: " + inner.Code, e.StackTrace);
					// WebGlobal.curEnv.stat = inner.Code;
					return ErrorStat(inner.Code, inner.Message);
				}
				else
				{
					Console.WriteLine("Exception: " + e.InnerException.Message);
					// string stat = "InternalServerError";
					// Logger.errLog.Write("Exception: " + e.InnerException.Message, e.StackTrace);
					// WebGlobal.curEnv.stat = stat;
					return ErrorStat("InternalServerError", "服务器内部异常");
				}
			}
			catch (NutsException e)
			{
				Console.WriteLine("NutsException: " + e.Code);
				// Logger.errLog.Write("NutsException: " + e.Code, e.StackTrace);
				// WebGlobal.curEnv.stat = e.Code;
				return ErrorStat(e.Code, e.Message);
			}
			catch (Exception e)
			{
				// string stat = "InternalServerError";
				Console.WriteLine("Exception: " + e.Message);
				// Logger.errLog.Write("Exception: " + e.Message, e.StackTrace);
				// WebGlobal.curEnv.stat = stat;
				return ErrorStat("InternalServerError", "服务器内部异常");
			}
		}

        public static JsonNode ErrorStat(string code, string message)
		{
			var jnStat = new JsonNode(code) { Name = "stat"};
            var jnMessage = new JsonNode(message) { Name = "message" };
			var jn = new JsonNode(JsonNodeType.Dictionary);
			jn.AddChildItem(jnStat);
            jn.AddChildItem(jnMessage);
            return jn;
		}
	}

	class SocketsDispatch : JmService
	{
		Dictionary<string, SocketsModule> m_mods = new Dictionary<string, SocketsModule>();
        ApiInvoker m_ivk;

		public delegate void NotifyDelegate(List<long> uids, JsonNode jnode);
		public NotifyDelegate Notify = null;

		public SocketsDispatch(ApiInvoker ivk)
        {
            m_ivk = ivk;
        }

        public void AddService(JmModule jmod)
		{
			var smod = new SocketsModule(jmod, m_ivk);
			m_mods.Add(jmod.BaseUrl.ToLowerInvariant(), smod);

			var mi = typeof(SocketsDispatch).GetMethod("InvokeNotifyVA", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			G.Verify(mi != null, "FailLinkNotify");
			smod.LinkNotify(this, mi);
		}

		public JsonNode Invoke(JsonNode jnode, SimpleChannel channel)
		{
			var jnM = jnode["sc:m"];
			string url = jnM.TextValue;

			int pos = url.LastIndexOf('/');
			if (pos < 0)
				return SocketsModule.ErrorStat("WrongMethod", "接口协议错误");
			string prefix = url.Substring(0, pos);

			var jnQ = jnode?.GetChildItem("sc:q");

			SocketsModule mod;
			if (m_mods.TryGetValue(prefix.ToLowerInvariant(), out mod))
				jnode = mod.Invoke(url, jnode);
			else
				jnode = SocketsModule.ErrorStat("MethodNotFound", "接口没有找到");

			var jnId = jnode.GetChildItem("sc:id");
			if (jnId != null && channel != null)
				channel.Uid = (int)jnId.IntValue;

			jnode.AddChildItem(jnM);
			if (jnQ != null)
				jnode.AddChildItem(jnQ);

			return jnode;
		}

		void InvokeNotifyVA(List<long> uids, JmNotify notify, params object[] args)
		{
			var jnode = m_ivk.BuildNotifyArgs(notify, args);
			Notify?.Invoke(uids, jnode);
		}

		public void Close(SimpleChannel channel)
		{
		}
	}
}
