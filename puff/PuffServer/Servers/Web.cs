using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Reflection;
using System.IO;
using Nano.Json;
using Nano.Ext.Web;
using Nano.Nuts;
using Puff.Model;
using Nano.Ext.Marshal;
using Nano.Logs;

namespace Puff.Servers
{
	interface WebMethod
	{
		void Dispatch(string url, HttpListenerContext ctx);
	}

	class StaticWebMethod : WebMethod
	{
		const string MSG_NOT_FOUND = "404 Not Found";

		public void Dispatch(string url, HttpListenerContext ctx)
		{
			var response = ctx.Response;
			string path = ".\\" + url;
			if (!File.Exists(path))
			{
				Console.WriteLine("Url=" + url + ", not found");
				ResponseWriter.SendResponse(ctx.Response, MSG_NOT_FOUND);
				return;
			}

			string ext = Path.GetExtension(path);
			string mime = GetMimeByExt(ext);
			response.ContentType = mime;

			FileStream istream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			response.ContentLength64 = istream.Length;
			Stream ostream = response.OutputStream;
			Nano.Net.ResponseReader.CopyStream(istream, ostream, new byte[0x10000]);
			ostream.Close();
			istream.Close();
		}

		static string GetMimeByExt(string ext)
		{
			switch (ext.ToLowerInvariant())
			{
				case ".txt":
					return "text/plain";
				case ".htm":
				case ".html":
					return "text/html";
				case ".ico":
					return "image/x-icon";
				case ".bmp":
					return "image/bmp";
				case ".gif":
					return "image/gif";
				case ".png":
					return "image/png";
				case ".jpg":
				case ".jpeg":
					return "image/jpeg";
				default:
					return "application/octet-stream";
			}
		}
	}

	class ApiWebMethod : WebMethod
	{
		JmModule m_box;
        ApiInvoker m_ivk;

		public ApiWebMethod(JmModule box, ApiInvoker ivk)
		{
			m_box = box;
            m_ivk = ivk;
		}

		public void Dispatch(string url, HttpListenerContext ctx)
		{
            var env = new Env(ctx);
            try
			{
				JmMethod m = m_box.GetMethod(url);
                
				if (m == null)
				{
                    Logger.Err("MethodNotFound", "Url=" + url);
                    WebGlobal.curEnv.stat = "MethodNotFound";
                    ResponseWriter.SendResponse(ctx.Response, ErrorStat("MethodNotFound", "接口没有找到"));
				}
				else if (m.Flags == IceApiFlag.Json)
				{
					DispatchJson(m, url, ctx);
				}
                else if (m.Flags == IceApiFlag.JsonIn)
                {
                    DispatchJsonIn(m, url, ctx);
                }
				else if (m.Flags == IceApiFlag.Http)
				{
					DispatchHttp(m, url, ctx);
				}
				else
				{
                    Logger.Err("UnsupportedMethodStyle", "Url=" + url);
                    WebGlobal.curEnv.stat = "UnsupportedMethodStyle";
                    ResponseWriter.SendResponse(ctx.Response, ErrorStat("UnsupportedMethodStyle", "不支持的接口风格"));
				}
			}
			catch (System.Reflection.TargetInvocationException e)
			{
				NutsException inner = e.InnerException as NutsException;
				if (inner != null)
				{
                    Logger.Err("NutsException: " + inner.Code, e.StackTrace);
                    WebGlobal.curEnv.stat = inner.Code;
                    ResponseWriter.SendResponse(ctx.Response, ErrorStat(inner.Code, inner.Message));
				}
				else
				{
                    Logger.Err("Exception: " + e.InnerException.Message, e.StackTrace);
                    string stat = "InternalServerError";
                    WebGlobal.curEnv.stat = stat;
                    ResponseWriter.SendResponse(ctx.Response, ErrorStat(stat, "服务器内部异常"));
				}
			}
			catch (NutsException e)
			{
                Logger.Err("NutsException: " + e.Code, e.StackTrace);
                WebGlobal.curEnv.stat = e.Code;
                ResponseWriter.SendResponse(ctx.Response, ErrorStat(e.Code, e.Message));
			}
			catch (Exception e)
			{
				string stat = "InternalServerError";
                Logger.Err("Exception: " + e.Message, e.StackTrace);
                WebGlobal.curEnv.stat = stat;
                ResponseWriter.SendResponse(ctx.Response, ErrorStat(stat, "服务器内部异常"));
			}
            _AccessLog(env);
           
        }

		#region Json Method

		public void DispatchJson(JmMethod m, string url, HttpListenerContext ctx)
		{
			Console.WriteLine("Json Api, Url=" + url);
			var request = ctx.Request;
			var response = ctx.Response;            

            var reqBody = ParseJsonRequestBody(request);
            var reqMap = ParseQueryMap(request);
            var args = m_ivk.PrepareJsonMethodArgs(m.MI, reqBody, reqMap);

            // var args = PrepareJsonMethodArgs(m.MI, request);
            var ret = m_ivk.Invoke(m, args);

            /*
            JsonNode jnode = m_ivk.BuildMethodReturn(m, ret);
            				
			BuildCookies(m, ret, response);
            JsonWriter wr = new JsonWriter(jnode, false);
			string retStr = wr.GetString();
            Console.WriteLine("return: " + retStr);
            WebGlobal.curEnv.AddLogParam("return", jnode);

            ResponseWriter.SendResponse(response, retStr);
            */

            var r = m_ivk.BuildJsonStyleApiReturn(m, ret);
            SendApiResponse(response, r);
        }

        public void DispatchJsonIn(JmMethod m, string url, HttpListenerContext ctx)
        {
            Console.WriteLine("Json Api, Url=" + url);
            var request = ctx.Request;
            var response = ctx.Response;

            var reqBody = ParseJsonRequestBody(request);
            var reqMap = ParseQueryMap(request);
            var args = m_ivk.PrepareJsonMethodArgs(m.MI, reqBody, reqMap);

            throw new NotImplementedException();
        }

        static JsonNode ParseJsonRequestBody(HttpListenerRequest request)
        {
            if (request.ContentLength64 != 0)
            {
                string str = RequestReader.GetPostText(request);
                WebGlobal.curEnv.postStr = str;
                var jnode = JsonParser.ParseText(str);
                G.Verify(jnode.NodeType == JsonNodeType.Dictionary, "WrongJsonType");
                return jnode;
            }
            return new JsonNode(JsonNodeType.Dictionary);
        }

        static Dictionary<string, string> ParseQueryMap(HttpListenerRequest request)
        {
            var queryMap = new Dictionary<string, string>();
            var pos = request.RawUrl.IndexOf('?');
            if (pos > 0)
            {
                // 内置的 QueryString 集合对中文解析会乱码
                var url = request.RawUrl.Substring(pos + 1);
                var query = System.Web.HttpUtility.UrlDecode(url);
                var qs = System.Web.HttpUtility.ParseQueryString(query);
                foreach (string key in qs.Keys)
                    queryMap.Add(key, qs[key]);
            }
            foreach (Cookie cookie in request.Cookies)
            {
                Console.WriteLine("cookie: " + cookie.Name + '=' + cookie.Value);
                queryMap.Add(cookie.Name, cookie.Value);
            }
            return queryMap;
        }

        static void SendApiResponse(HttpListenerResponse response, IceApiResponse r)
        {
            if (r.Cookies != null)
            {
                foreach (var pair in r.Cookies)
                {
                    Cookie cookie = new Cookie(pair.Key, pair.Value);
                    cookie.Path = "/";  // 设置所有cookie默认path为根
                    response.AppendCookie(cookie);
                }
            }
            if (r.Json != null)
            {
                G.Verify(r.Data == null && r.Stream == null, "DuplicatedReturn");
                response.ContentType = "application/json";
                var jw = new JsonWriter(r.Json, false);
                var str = jw.GetString();
                ResponseWriter.SendResponse(response, str, r.HttpStatusCode);
            }
            else if (r.Data != null)
            {
                G.Verify(r.Stream == null, "DuplicatedReturn");
                ResponseWriter.SendResponse(response, r.Data, 0, r.Data.Length, r.HttpStatusCode);
            }
            else
            {
                G.Verify(r.Stream != null, "NoReturn");
                ResponseWriter.SendResponse(response, r.Stream, r.HttpStatusCode);
            }
        }

        /*
		internal object[] PrepareJsonMethodArgs(MethodInfo m, HttpListenerRequest request)
		{
			JsonNode jnode = null;
			if (request.ContentLength64 != 0)
			{
				string str = RequestReader.GetPostText(request);
                WebGlobal.curEnv.postStr = str;
                Console.WriteLine("body: " + str);
				jnode = JsonParser.ParseText(str);
				G.Verify(jnode.NodeType == JsonNodeType.Dictionary, "WrongJsonType");
			}

			var cookies = new Dictionary<string, string>();
			foreach (Cookie cookie in request.Cookies)
			{
				Console.WriteLine("cookie: " + cookie.Name + '=' + cookie.Value);
				cookies.Add(cookie.Name, cookie.Value);
			}
            if (request.HttpMethod == "GET")
            {
                var get_params = new Dictionary<string, object>();
                foreach (var key in request.QueryString.AllKeys)
                {
                    get_params[key] = request.QueryString.GetValues(key)[0];
                }
                jnode = JsonModel.Dump(get_params);
            }

            WebGlobal.curEnv.AddLogParam("reqParams", jnode);
            WebGlobal.curEnv.AddLogParam("cookie", JsonModel.Dump(cookies));
            return m_ivk.PrepareJsonMethodArgs(m, jnode, cookies);
		}
        */

        /*
		internal void BuildCookies(JmMethod m, object ret, HttpListenerResponse response)
		{
			if (m.Cookies == null || m.Cookies.Length == 0)
				return;

			if (m.Rets.Length > 1)
			{
				var map = new Dictionary<string, object>();
				if (ret is object[])
					BuildMapFromObjectArray(m.Rets, (object[])ret, map);
				else
					BuildMapFromTuple(m.Rets, ret, map);

				foreach (var name in m.Cookies)
				{
					Cookie cookie = new Cookie(name, map[name].ToString());
                    cookie.Path = "/";// 设置所有cookie默认path为根
					response.AppendCookie(cookie);
				}
			}
			else if (m.Rets.Length == 1)
			{
				G.Verify(m.Cookies.Length == 1 && m.Cookies[0] == m.Rets[0], NutsException.KeyNotFound);
				var cookie = new Cookie(m.Cookies[0], ret.ToString());
                cookie.Path = "/";// 设置所有cookie默认path为根
                response.AppendCookie(cookie);
			}
			else
				throw new NutsException(NutsException.OutOfRange);
		}

		internal void BuildMapFromObjectArray(string[] names, object[] ret, Dictionary<string, object> map)
		{
			for (int i = 0; i < names.Length; ++i)
				map.Add(names[i], ret[i]);
		}

		internal void BuildMapFromTuple(string[] names, object ret, Dictionary<string, object> map)
		{
			G.Verify(names.Length < 8, NutsException.OutOfRange);
			var vt = ret.GetType();
			for (int i = 0; i < names.Length; ++i)
			{
				var m = vt.GetProperty("Item" + (i + 1));
				var o = m.GetValue(ret, null);
				map.Add(names[i], o);
			}
		}
        */

		#endregion

		public void DispatchHttp(JmMethod m, string url, HttpListenerContext ctx) => m.MI.Invoke(m.Instance, new object[] { url, ctx });

		static string ErrorStat(string stat, string message) => "{\"stat\":\"" + stat + "\", \"message\":\"" + message + "\"}";

        private void _AccessLog(Env env)
        {
            char SEP = '\t';
            StringBuilder sb = new StringBuilder();
            sb.Append(env.ip);
            sb.Append(SEP);
            sb.Append(env.reqId);
            sb.Append(SEP);
            sb.Append(UnixTimestamp.GetUtcNowTimeValue() - env.startTime);
            sb.Append(SEP);
            sb.Append(env.ctx.Request.Url.AbsolutePath);
            sb.Append(SEP);
            sb.Append(env.stat);
            sb.Append(SEP);
            sb.Append(env.postStr);
            if (env.logParams != null)
            {
                foreach (var param in env.logParams)
                {
                    sb.Append(SEP);
                    JsonWriter wr = new JsonWriter(param.Value, false);
                    string retStr = wr.GetString();
                    sb.Append(param.Key + ":");
                    sb.Append(retStr);
                }
                
            }
            Logger.Acc(null, sb.ToString());
        }
	}

	class WebDispatch : IUrlDispatch
	{
		Dictionary<string, WebMethod> Methods = new Dictionary<string, WebMethod>();

        ApiInvoker m_ivk;
		StaticWebMethod m_static;

        public WebDispatch(ApiInvoker ivk)
        {
            m_ivk = ivk;
            m_static = new StaticWebMethod();
        }

		void AddMethod(string name, WebMethod method) => Methods.Add(name.ToLowerInvariant(), method);

		public void AddService(JmModule jms) => AddMethod(jms.BaseUrl, new ApiWebMethod(jms, m_ivk));

		public void AddStatic(string name) => AddMethod(name, m_static);

		public void Dispatch(string url, HttpListenerContext ctx)
		{
#if true
			int pos = url.LastIndexOf('/');
			string prefix = pos > 0 ? url.Substring(0, pos) : url;
			WebMethod m;
			if (Methods.TryGetValue(prefix.ToLowerInvariant(), out m))
			{
				m.Dispatch(url.ToLowerInvariant(), ctx);
			}
			else
			{
				Console.WriteLine("MethodNotFound, Url=" + url);
				ResponseWriter.SendResponse(ctx.Response, "{\"stat\":\"MethodNotFound\"}");
			}
#else
			foreach (var pair in Methods)
			{
				if (url.StartsWith(pair.Key))
				{
					pair.Value.Dispatch(url.ToLowerInvariant(), ctx);
					return;
				}
			}
			Console.WriteLine("MethodNotFound, Url=" + url);
			ResponseWriter.SendResponse(ctx.Response, "{\"stat\":\"MethodNotFound\"}");
#endif
		}
	}

    public class Env
    {
        public string ip;
        public string stat;
        public string reqId;
        public long startTime;
        public string postStr;
        public HttpListenerContext ctx;
        public Dictionary<string, JsonNode> logParams;
        public Env(HttpListenerContext ctx)
        {
            startTime = UnixTimestamp.GetUtcNowTimeValue();
            reqId = Convert.ToBase64String(Guid.NewGuid().ToByteArray(), 0, 12);
            this.ctx = ctx;
            ip = ctx.Request.Headers.Get("X-Forwarded-For");
            if (ip == null)
                ip = ctx.Request.RemoteEndPoint.Address.ToString();

            ctx.Response.AddHeader("X-ReqId", this.reqId);
            logParams = new Dictionary<string, JsonNode>();
            WebGlobal.curEnv = this;
        }

        public void AddLogParam(string key, JsonNode param)
        {
            logParams.Add(key, param);
        }

    }

    public class WebGlobal
    {
        [ThreadStatic]
        public static Env curEnv;
    }
}
