using Nano.Json.Expression;
using Nano.Xapi.Netdisk.Api;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Impls
{
    class XsvrQuota : XApiModule, IQuotaModule
    {
        public NdGetUserQuotaResponse GetUserQuota(XHttpRequest<NdGetUserQuotaResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/quota/api/getUserQuota");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.EDict();
            return new XHttpRequest<NdGetUserQuotaResponse>(Client, invoker).Send(url, e, callback);
        }
    }
}
