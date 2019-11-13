using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Api;
using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Json.Expression;

namespace Nano.Xapi.Netdisk.Impls
{
    class XAudit : XApiModule, IAuditModule
    {
        public NdResponse recordSyncAudit(string type, string localPath, string serverPath, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/audit/api/recordSyncAudit");
            JE e = JE.New() + JE.Dict() + 
                JE.Pair("token", Ctx.Token) + 
               JE.Pair("type", type) +
               JE.Pair("local", localPath) +
               JE.Pair("server", serverPath) +
               JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

    }
}
