using Nano.Xapi.Netdisk.Api;
using Nano.Xapi.Netdisk.Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Error;

namespace Nano.Xapi.Netdisk
{
    public class XApiContext
    {
        public string Server;
        public string Token;
        public bool Online;
        public bool PrintLog;
        public bool Pathable;
    }

    public class XApiModule
    {
        public XApiContext Ctx;
        public XAgent Client;

        public void Init(XAgent c, XApiContext ctx)
        {
            Client = c; Ctx = ctx;
        }

        public string MakeUrl(string path)
        {
            return Ctx.Server + path;
        }
    }

    public class XApiBuilder
    {
        Dictionary<string, object> Modules = new Dictionary<string, object>();

        public T Get<T>()
        {
            var tlc = typeof(T).ToString().ToLowerInvariant();
            object impl;
            var ret = Modules.TryGetValue(tlc, out impl);
            if(ret && (impl is T))
                return (T)impl;

            return default(T);
        }

        public void Register<T>(object t)
        {
            var tlc = typeof(T).ToString().ToLowerInvariant();
            Modules.Add(tlc, t);
        }
    }

    public class XAgent : XHttpClient
    {
        public XApiContext Context { get; protected set; }
        public ISvrFileSystem fs { get; protected set; }
        public XUfa ufa { get; protected set; }
        public IQuotaModule quota { get; protected set; }
        public IAuthModule auth { get; protected set; }
        public IUserSystem user { get; protected set; }
        public IACLModule acl { get; protected set; }

        public IPathFileSystem file { get; protected set; }

        public IAuditModule audit { get; protected set; }


        public void Init(ServerConfig cfg, ProxyConfig proxy)
        {
            Context = new XApiContext();
            Context.Online = true;
            Context.Server = CombineUrl(cfg);

            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            if (cfg.Protocal.ToLowerInvariant() == "https")
            {
                InitHttpsEnv();
            }

            if (proxy != null)
            {
                Proxy = XHttpProxy.Create(proxy);
            }
        }

        public void Setup(XApiBuilder builder)
        {
            this.fs = builder.Get<ISvrFileSystem>();
            this.ufa = builder.Get<XUfa>();
            this.auth = builder.Get<IAuthModule>();
            this.quota = builder.Get<IQuotaModule>();
            this.user = builder.Get<IUserSystem>();
            this.acl = builder.Get<IACLModule>();
            this.file = builder.Get<IPathFileSystem>();
            this.audit = builder.Get<IAuditModule>();
        }

        public void Setup()
        {
            var fs = new Impls.XsvrFileSystem();
            fs.Init(this, Context);
            this.fs = fs;

            var ufa = new Impls.XsvrUfa();
            ufa.Init(this, Context);
            this.ufa = ufa;

            var quota = new Impls.XsvrQuota();
            quota.Init(this, Context);
            this.quota = quota;

            var auth = new Impls.XsvrAuth();
            auth.Init(this, Context);
            this.auth = auth;

            var user = new Impls.XsvrUser();
            user.Init(this, Context);
            this.user = user;

            var acl = new Impls.XsvrACL();
            acl.Init(this, Context);
            this.acl = acl;

            var file = new Impls.XPathFileSystem();
            file.Init(this, Context);
            this.file = file;

            var audit = new Impls.XAudit();
            audit.Init(this, Context);
            this.audit = audit;
        }

        public override void OnTryTimeout(XBaseRequest req)
        {
            Context.Online = false;
        }

        protected virtual void InitHttpsEnv()
        {
            //Debug.Assert(false);
            //throw new NotImplementedException();
        }

        string CombineUrl(ServerConfig cfg)
        {
            var port = cfg.Port <= 0 ? 80 : cfg.Port;
            return string.Format("{0}://{1}:{2}", cfg.Protocal, cfg.Host, port);
        }
    }
}
