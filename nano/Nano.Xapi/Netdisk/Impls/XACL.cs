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
    public class XsvrACL : XApiModule, IACLModule
    {
        public NdListGroupResponse ListGroup(XHttpRequest<NdListGroupResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/acl/api/listGroup");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.EDict();
            return new XHttpRequest<NdListGroupResponse>(Client, invoker).Send(url, e, callback);
        }
    }
}
