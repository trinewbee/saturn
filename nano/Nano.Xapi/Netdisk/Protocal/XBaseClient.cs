using Nano.Xapi.Netdisk.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XBaseInvoker : IBaseInvoker
    {
        public void Run(Action action, bool isAsync)
        {
            if (isAsync)
                System.Threading.Tasks.Task.Factory.StartNew(action);
            else
                action.Invoke();
        }

        public void Call<TReq, TResp>(Action<TReq, TResp> action, TReq req, TResp resp)
        {
            action?.Invoke(req, resp);
        }
    }

    public abstract class XBaseClient
    {
        public bool Logable;

        public XBaseInvoker Invoker { get; protected set; }

        public abstract IBaseMethod Create(string body);

        public abstract IBaseMethod Create(byte[] data, int offset, int length);

        public abstract void Abort(XBaseRequest req);

        public abstract void OnTryTimeout(XBaseRequest req);

        public abstract void Invoke(XBaseRequest req, IBaseMethod method);

        protected void SetTimeout(XBaseRequest req)
        {
            OnTryTimeout(req);
        }
    }
}
