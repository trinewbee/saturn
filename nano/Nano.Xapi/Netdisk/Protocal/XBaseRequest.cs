using Nano.Xapi.Netdisk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Protocal
{

    public interface IBaseMethod
    {
        void Invoke(XBaseRequest req);
    }


    public interface IBaseInvoker
    {
        /// <summary>
        /// 可用来控制多线程执行网络访问
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isAsync"></param>
        void Run(Action action, bool isAsync);

        /// <summary>
        /// 用来执行Request的回调，重载此方法可以以UI线程进行回调。
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TResp"></typeparam>
        /// <param name="action"></param>
        /// <param name="req"></param>
        /// <param name="resp"></param>
        void Call<TReq, TResp>(Action<TReq, TResp> action, TReq req, TResp resp);
    }

    public class XBaseRequest
    {
        public XBaseClient Client { get; protected set; }
        public IBaseMethod Method { get; protected set; }
        public IBaseInvoker Invoker { get; protected set; }

        public string Url { get; protected set; }
        public string Result { get; internal set; }
        public bool IsAborted { get; protected set; }
        public int Timeout { get; set; }
        public bool KeepAlive { get; set; }
        public List<Range> Ranges { get; protected set; }

        private object m_data;
        private object m_lifeLocker = new object();


        public virtual XBaseResponse GetResponse()
        {
            throw new NotImplementedException();
        }

        public void Error(string stat, string message = "")
        {
            lock (m_lifeLocker)
            {
                GetResponse().Error(stat, message);
            }
        }

        public void Abort()
        {
            lock (m_lifeLocker)
            {
                if (!IsAborted)
                {
                    OnAbort();
                    IsAborted = true;
                }
            }
        }

        public void Attach(object obj)
        {
            lock (m_lifeLocker) { m_data = obj; }
        }

        public object Dettach()
        {
            return m_data;
        }

        protected virtual void OnAbort() { }
    }
}
