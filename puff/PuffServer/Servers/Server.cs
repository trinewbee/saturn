using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Ext.Web;
using Nano.Logs;
using Nano.Nuts;
using Puff.Marshal;
using Puff.Model;

namespace Puff.Servers
{
	public class PuffServer
	{
        ApiInvoker m_invoker;
        WebDispatch m_webdsp;
        SocketsDispatch m_sckdsp;
		MiniServer m_websvr = null;
        Nano.Sockets.JmServer m_scksvr = null;

        public PuffServer()
        {
            var job = JsonObjectBuilder.BuildDefault();
            var jmb = JsonModelBuilder.BuildDefault();
            m_invoker = new ApiInvoker(job, jmb);
            m_webdsp = new WebDispatch(m_invoker);
            m_sckdsp = new SocketsDispatch(m_invoker);
        }

		public void AddService(object o)
		{
			var jmod = new JmModule();
			jmod.Init(o);
			m_webdsp.AddService(jmod);
			m_sckdsp.AddService(jmod);
		}

		public void AddStatic(string name)
		{
			m_webdsp.AddStatic(name);
		}

		public void StartServer(uint portWeb = 0, uint portSockets = 0, uint portWebSock = 0)
		{
			G.Verify(m_websvr == null, NutsException.AlreadyOpen);
            Logger.ChecLogIsInit();
			if (portWeb != 0)
			{
				m_websvr = new MiniServer(m_webdsp);
				m_websvr.Start(portWeb);
			}
            if (portSockets != 0 || portWebSock != 0)
            {
				m_scksvr = new Nano.Sockets.JmServer();
				m_sckdsp.Notify += m_scksvr.Notify;
                m_scksvr.Start(m_sckdsp, (int)portSockets, (int)portWebSock);
            }
		}

		public void StopServer()
		{
			if (m_websvr != null)
			{
				m_websvr.Close();
				m_websvr = null;
			}
		}
	}
}
