using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Model;
using Nano.Json.Expression;
using Nano.Xapi.Netdisk.Api;

namespace Nano.Xapi.Netdisk.Impls
{
    public class XsvrAuth : XApiModule, IAuthModule
    {
        public NdLoginResponse NameLogin(string name, string pass, XHttpRequest<NdLoginResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/auth/api/nameLogin");
            JE e = JE.New() + JE.Dict() + JE.Pair("name", name) + JE.Pair("password", pass) + JE.EDict();
            return new XHttpRequest<NdLoginResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse Logout(XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/auth/api/logout");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }
    }
}
