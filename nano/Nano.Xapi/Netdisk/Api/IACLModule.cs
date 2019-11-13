using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Model;
using Nano.Json.Expression;

namespace Nano.Xapi.Netdisk.Api
{
    public interface IACLModule
    {
        NdListGroupResponse ListGroup(XHttpRequest<NdListGroupResponse>.XCallback callback = null, IBaseInvoker invoker = null);
    }
}
