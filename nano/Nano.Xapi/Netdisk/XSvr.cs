using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Error;
using Nano.Xapi.Netdisk.Api;

namespace Nano.Xapi.Netdisk
{
    public class XSvr
    {
        public static XAgent Agent { get; private set; }

        public static ISvrFileSystem fs { get { return Agent.fs; } }

        public static IPathFileSystem file { get { return Agent.file; } }

        public static XUfa ufa { get { return Agent.ufa; } }

        public static IQuotaModule quota { get { return Agent.quota; } }

        public static IAuthModule auth { get { return Agent.auth; } }

        public static IUserSystem user { get { return Agent.user; } }

        public static IACLModule acl { get { return Agent.acl; } }

        public static IAuditModule audit { get { return Agent.audit; } }


        public static void Init(ServerConfig cfg, ProxyConfig proxy, bool printLog = true)
        {
            var client = new XAgent();
            client.Init(cfg, proxy);
            client.Setup();
            Init(client, printLog);
        }

        public static void Init(XAgent agent, bool printLog)
        {
            Agent = agent;
            agent.Context.PrintLog = printLog;
            agent.Logable = printLog;
        }

        public static void SetToken(string token)
        {
            CheckInit();
            Agent.Context.Token = token;
        }

        public static void SetOnline(bool isOnline)
        {
            CheckInit();
            Agent.Context.Online = isOnline;
        }

        public static void CheckConnection(string type = null)
        {
            if (Agent != null)
                XE.Verify(XSvr.Agent.Context.Online, XStat.ERROR_UNEXP_NET_ERR, "XSvr Check Connection, off line");
        }

        public static bool CheckInit(bool verify = true)
        {
            var ret = Agent != null;
            XE.Verify(ret || !verify, XStat.ERROR_INTERNAL_ERROR, "XSvr is not inited");
            return ret;
        }
    }
}
