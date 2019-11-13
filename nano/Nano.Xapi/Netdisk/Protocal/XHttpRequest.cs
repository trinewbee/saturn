using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nano.Json.Expression;
using System.Diagnostics;
using Nano.Ext.Logs;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XHttpRequest<TResponse> : XBaseRequest where TResponse : XBaseResponse, new()
    {
        public delegate void XCallback(XHttpRequest<TResponse> req, TResponse resp);

        public XCallback Callback { get; protected set; }
        public TResponse Response { get; protected set; }

        private Watcher m_watcher = new Watcher();

        public XHttpRequest(XBaseClient client, IBaseInvoker invoker)
        {
            Client = client;
            Ranges = new List<Range>();
            Response = new TResponse();
            Invoker = invoker ?? client.Invoker;
        }

        public TResponse Send(string url, JE expr, XCallback callback = null)
        {
            var body = expr == null ? null : expr.GetString();
            var method = Client.Create(body);
            return Send(url, method, callback);
        }

        public TResponse Send(string url, string body, XCallback callback = null)
        {
            var method = Client.Create(body);
            return Send(url, method, callback);
        }

        public TResponse Send(string url, byte[] data, int offset, int len, XCallback callback = null)
        {
            var method = Client.Create(data, offset, len);
            return Send(url, method, callback);
        }

        public TResponse Send(string url, IBaseMethod method, XCallback callback = null)
        {
            Url = url;
            Callback = callback;
            Method = method;
            Response.Init(this);
            m_watcher.Start();
            Invoker.Run(Invoke, callback != null);
            return (TResponse)GetResponse();
        }

        protected void Invoke()
        {
            try
            {
                Client.Invoke(this, Method);

                if (Client.Logable)
                    m_watcher.Print(Url, Method.ToString(), Result);

                var resp = (TResponse)GetResponse();
                if (Callback != null)
                    Invoker.Call(Callback.Invoke, this, resp);
            }
            catch (System.Exception e)
            {
                Log.e(e, "[XHttpRequest.Invoke] Failed");
                Debug.Assert(false);
            }
        }

        public override XBaseResponse GetResponse()
        {
            return Response;
        }

        protected override void OnAbort()
        {
            Client.Abort(this);
        }

        class Watcher
        {
            private Stopwatch m_watch = new Stopwatch();

            static Regex ms_tokenReg = new Regex("\\\"token\\\"\\:\\\"[^\\\"]*\\\"");
            static string ms_replaceToken = "\"token\":\"********\"";

            static Regex ms_pwdReg = new Regex("\\\"password\\\"\\:\\\"[^\\\"]*\\\"");
            static string ms_pwdReplace = "\"password\":\"********\"";

            public void Start()
            {
                m_watch.Start();
            }

            public void Print(string url, string body, string data)
            {
                body = body == null ? null : ms_tokenReg.Replace(body, ms_replaceToken);
                data = data == null ? null : ms_tokenReg.Replace(data, ms_replaceToken);

                body = body == null ? null : ms_pwdReg.Replace(body, ms_pwdReplace);
                data = data == null ? null : ms_pwdReg.Replace(data, ms_pwdReplace);

                m_watch.Stop();
                Log.i("[HttpRequest] {0} with {1}, data is {2}, time={3}", url, body, data, m_watch.ElapsedMilliseconds);

            }
        }
    }
}
