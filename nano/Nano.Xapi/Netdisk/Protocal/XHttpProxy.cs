using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XHttpProxy
    {
        public const int TYPE_NONE = 0;
        public const int TYPE_SOCK4 = 1;
        public const int TYPE_SOCK5 = 2;
        public const int TYPE_IE = 3;

        int m_type = 0;
        IWebProxy m_baseProxy;
        ProxyConfig m_cfg;

        public static XHttpProxy Create(ProxyConfig cfg)
        {
            XHttpProxy proxy = new XHttpProxy();
            proxy.m_cfg = cfg;
            if (proxy.OnCreate(cfg))
            {
                return proxy;
            }
            return null;
        }

        public void Proxy(HttpWebRequest req)
        {
            if (m_type == 0)//none
            {
                return;
            }
            req.Proxy = m_baseProxy;
        }

        bool OnCreate(ProxyConfig cfg)
        {
            m_type = cfg.Type;
            m_baseProxy = WebRequest.DefaultWebProxy = CreateProxy(cfg);
            return true;
        }

        IWebProxy CreateProxy(ProxyConfig cfg)
        {
            switch (cfg.Type)
            {
                case TYPE_NONE: return null;
                case TYPE_SOCK4: return CreateProxySock4(cfg);
                case TYPE_SOCK5: return CreateProxySock5(cfg);
                case TYPE_IE: return CreateProxyIE(cfg);
                default: return null;
            }
        }

        IWebProxy CreateProxySock4(ProxyConfig cfg)
        {
            var proxy = new WebProxy(cfg.Host, (int)cfg.Port);
            proxy.Credentials = new NetworkCredential(cfg.User, cfg.Password);
            return proxy;
        }

        IWebProxy CreateProxySock5(ProxyConfig cfg)
        {
            var proxy = new WebProxy(cfg.Host, (int)cfg.Port);
            proxy.Credentials = new NetworkCredential(cfg.User, cfg.Password);
            return proxy;
        }

        IWebProxy CreateProxyIE(ProxyConfig cfg)
        {
            return WebRequest.GetSystemWebProxy();
        }
    }
}
