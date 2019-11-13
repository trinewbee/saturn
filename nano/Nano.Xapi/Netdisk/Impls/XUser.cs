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
    class XsvrUser : XApiModule, IUserSystem
    {
        public NdGetUserInfoResponse GetUserInfo(XHttpRequest<NdGetUserInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/user/api/getUserInfo");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.EDict();
            return new XHttpRequest<NdGetUserInfoResponse>(Client, invoker).Send(url, e, callback);
        }
    }
}
