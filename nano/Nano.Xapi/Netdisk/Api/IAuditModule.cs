using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Api
{
    public interface IAuditModule
    {
        NdResponse recordSyncAudit(string type, string localPath, string serverPath, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);
    }
}
