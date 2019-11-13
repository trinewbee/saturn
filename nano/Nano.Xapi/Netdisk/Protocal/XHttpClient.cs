using Nano.Xapi.Netdisk.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Nano.Json.Expression;
using Nano.Ext.Error;
using Nano.Ext.Logs;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XHttpClient : XBaseClient
    {
        public const string MimeForm = "application/x-www-form-urlencoded";
        public const string MimeBinary = "application/octet-stream";

        public XHttpProxy Proxy { get; protected set; }
        public CookieContainer Cookies { get; protected set; }
        public Dictionary<string, string> Headers { get; protected set; }

        public XHttpClient()
        {
            Proxy = null;
            Headers = new Dictionary<string, string>();
            Cookies = new CookieContainer();
            Invoker = new XBaseInvoker();
        }

        public override IBaseMethod Create(string body)
        {
            if (body == null)
                return new XHttpSimpleGetter(this);

            var method = new XHttpStringPoster(this);
            method.Init(body);
            return method;
        }

        public override IBaseMethod Create(byte[] data, int offset, int length)
        {
            if (data == null)
                return new XHttpSimpleGetter(this);

            var method = new XHttpBufferPoster(this);
            method.Init(data, offset, length);
            return method;
        }

        public override void Abort(XBaseRequest req)
        {
            var httpr = req.Dettach() as HttpWebRequest;
            if (httpr != null)
                httpr.Abort();
        }

        public override void Invoke(XBaseRequest req, IBaseMethod method)
        {
            int nTryTime = 3;
            while (!req.IsAborted)
            {
                nTryTime--;
                try
                {
                    method.Invoke(req);
                    return;
                }
                catch (System.Net.WebException ex)
                {
                    Log.e(ex, "XHttClient.Invoke Failed: {0} => {1}", req.Url, method.ToString());
                    var ret = XE.Make(ex, null);
                    if (ret.Stat != XStat.ERROR_UNEXP_NET_ERR)
                    {
                        req.Error(ret.Stat, ret.Message);
                        return;
                    }
                    else if (nTryTime <= 0)
                    {
                        req.Error(ret.Stat, ret.Message);
                        SetTimeout(req);
                        return;
                    }
                }
                catch (XError ex)
                {
                    Log.e(ex, "XHttClient.Invoke Failed: {0} => {1}", req.Url, method.ToString());
                    req.Error(ex.Stat, ex.Message);
                    return;
                }
                catch (System.Exception ex)
                {
                    Log.e(ex, "XHttClient.Invoke Failed: {0} => {1}", req.Url, method.ToString());
                    req.Error(XStat.ERROR_INTERNAL_ERROR, ex.Message);
                    return;
                }
            }
            req.Error(XStat.ERROR_CONNECTION_ABORTED);
        }

        public override void OnTryTimeout(XBaseRequest req)
        {

        }

        public HttpWebRequest CreateRequest(string url, string method, int timeout = 0, bool keepAlive = false)
        {
            System.GC.Collect();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = Cookies;
            request.Method = method.ToUpper();
            request.ContentType = MimeBinary;
            if (timeout > 0)
                request.Timeout = timeout;
            request.KeepAlive = keepAlive;
            return request;
        }

        public void InitHeaders(XBaseRequest req)
        {
            var httpr = req.Dettach() as HttpWebRequest;
            XE.Verify(httpr != null, XStat.ERROR_CONNECTION_ABORTED);

            var headers = Headers;
            foreach (var h in headers)
            {
                httpr.Headers.Add(h.Key, h.Value);
            }
            
            req.Ranges.ForEach(x => httpr.AddRange(x.From, x.To - 1));
        }

        public void BindProxy(XBaseRequest req)
        {
            var httpr = req.Dettach() as HttpWebRequest;
            XE.Verify(httpr != null, XStat.ERROR_CONNECTION_ABORTED);
            Proxy?.Proxy(httpr);
        }

        public int PostData(XBaseRequest req, byte[] data, int offset, int len)
        {
            var httpr = req.Dettach() as HttpWebRequest;
            XE.Verify(httpr != null, XStat.ERROR_CONNECTION_ABORTED);
            System.IO.Stream stream = null;
            try
            {
                stream = httpr.GetRequestStream();
                stream.Write(data, offset, len);
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return len;
        }

        public void FetchData(XBaseRequest req)
        {
            var resp = req.GetResponse();
            resp.Parse();
        }
    }
}
