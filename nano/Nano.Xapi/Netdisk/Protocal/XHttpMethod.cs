using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Error;

namespace Nano.Xapi.Netdisk.Protocal
{
    public abstract class XHttpGetter : IBaseMethod
    {
        protected XHttpClient Client;

        public XHttpGetter(XHttpClient client)
        {
            Client = client;
        }

        public abstract void Invoke(XBaseRequest req);

        public override string ToString()
        {
            return GetType().FullName;
        }
    }

    public abstract class XHttpPoster : IBaseMethod
    {
        protected XHttpClient Client;

        public byte[] Data { get; protected set; }

        public int Offset { get; protected set; }

        public int Length { get; protected set; }

        public XHttpPoster(XHttpClient client)
        {
            Client = client;
        }

        public abstract void Invoke(XBaseRequest req);

        public void Init(byte[] data, int offset, int len)
        {
            Data = data;
            Offset = offset;
            Length = len;
        }

        public override string ToString()
        {
            return string.Format("Length:{0}, Offset:{0}", Length, Offset);
        }
    }

    public class XHttpBufferPoster : XHttpPoster
    {
        public XHttpBufferPoster(XHttpClient c) : base(c) { }

        public override void Invoke(XBaseRequest req)
        {
            var c = Client;
            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            var httpr = c.CreateRequest(req.Url, "POST", req.Timeout, req.KeepAlive);
            req.Attach(httpr);

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            c.InitHeaders(req);
            c.BindProxy(req);

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            httpr.ContentLength = Length;
            var posted = c.PostData(req, Data, Offset, Length);
            XE.Verify(posted == Length, XStat.ERROR_INTERNAL_ERROR);

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            c.FetchData(req);
            
            httpr = null;
        }
    }

    public class XHttpStringPoster : XHttpBufferPoster
    {
        public string Body { get; protected set; }

        public XHttpStringPoster(XHttpClient c) : base(c) { }

        public void Init(string body)
        {
            Body = body;
            var data = Encoding.UTF8.GetBytes(body);
            Init(data, 0, data.Length);
        }

        public override string ToString()
        {
            return Body;
        }
    }

    public class XHttpSimpleGetter : XHttpGetter
    {
        public XHttpSimpleGetter(XHttpClient c) : base(c) { }

        public override void Invoke(XBaseRequest req)
        {
            XHttpClient c = Client;

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            var httpr = c.CreateRequest(req.Url, "GET", req.Timeout, req.KeepAlive);
            req.Attach(httpr);

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            c.InitHeaders(req);
            c.BindProxy(req);

            XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
            c.FetchData(req);

            httpr = null;
        }
    }    

}
