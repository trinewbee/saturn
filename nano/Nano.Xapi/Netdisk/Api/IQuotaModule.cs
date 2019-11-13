using Nano.Json.Expression;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Api
{
    public interface IQuotaModule
    {
        NdGetUserQuotaResponse GetUserQuota(XHttpRequest<NdGetUserQuotaResponse>.XCallback callback = null, IBaseInvoker invoker = null);
    }
}
