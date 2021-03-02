using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nano.Collection;
using Nano.Ext.Marshal;
using Nano.Json;
using Nano.Logs;
using Nano.Nuts;
using Puff.Marshal;
using Puff.Ext.Sentry;
using System.Security.Cryptography;

namespace Puff.NetCore
{
    class JmMethod
    {
        public string Name;
        public MethodInfo MI;
        public IceApiAttribute Attr;
        public string StatKey;
        public string[] Rets;
        public string[] Cookies;
    }

    class JmModule
    {
        Dictionary<string, JmMethod> Map = null;

        public void BuildJmMethodMap(Type vt)
        {
            Map = new Dictionary<string, JmMethod>();
            var methods = vt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var mi in methods)
            {
                var attrs = mi.GetCustomAttributes(typeof(IceApiAttribute), false);
                if (attrs.Length == 0)
                    continue;

                G.Verify(attrs.Length < 2, "DupIceApiAttr");
                var jm = BuildJmMethod(mi, (IceApiAttribute)attrs[0]);
                Map.Add(jm.Name, jm);
            }
        }

        JmMethod BuildJmMethod(MethodInfo mi, IceApiAttribute attr)
        {
            var jm = new JmMethod { Name = mi.Name.ToLowerInvariant(), MI = mi, Attr = attr };

            jm.StatKey = attr.Stat;
            jm.Rets = attr.Ret != null ? attr.Ret.Split(',') : new string[0];
            jm.Cookies = attr.Cookie?.Split(',');

            Check(jm);

            return jm;
        }

        static void Check(JmMethod jm)
        {
            var flag = jm.Attr.Flags;
            if (flag == IceApiFlag.Json)
            {
                CheckJsonRet(jm);
            }
            else if (flag == IceApiFlag.JsonIn)
            {
                CheckApiRet(jm.MI);
            }
            else if (flag == IceApiFlag.Http)
            {
                CheckApiIn(jm.MI);
                CheckApiRet(jm.MI);
            }
        }

        static void CheckApiIn(MethodInfo mi)
        {
            var args = mi.GetParameters();
            G.Verify(args.Length == 1, "WrongArgCount");
            G.Verify(args[0].ParameterType == typeof(IceApiRequest), "WrongArgType");
        }

        static void CheckJsonRet(JmMethod jm)
        {
            var vt = jm.MI.ReturnType;
            if (jm.Rets.Length > 1)
            {
                if (vt == typeof(object[]))
                    return;

                int n = GetTupleCount(vt);
                G.Verify(jm.Rets.Length == n, "WrongReturnItemNumber");
            }
            else if (vt.FullName.StartsWith("System.ValueTuple`"))
            {
                var names = MethodOutBuilder2.RetrieveReturnValueTupleNames(jm.MI);
                G.Verify(names != null, "NameMissInValueTuple");
                // jm.Rets = names.ToArray();  // not available in .net core
                jm.Rets = CollectionKit.ToArray(names);
            }
        }

        static void CheckApiRet(MethodInfo mi)
        {
            var vt = mi.ReturnType;
            G.Verify(vt == typeof(IceApiResponse), "WrongRetType");
        }

        static int GetTupleCount(Type vt)
        {
            const string prefix = "System.Tuple`";
            var gt = vt.GetGenericTypeDefinition();
            G.Verify(gt.FullName.StartsWith(prefix), "NotTupleType");

            // Nested tuple not supported
            return int.Parse(gt.FullName.Substring(prefix.Length));
        }

        public JmMethod GetJmMethod(string name)
        {
            JmMethod jm;
            if (Map.TryGetValue(name.ToLowerInvariant(), out jm))
                return jm;
            return null;
        }
    }

    static class JmGlobal
    {
        static Dictionary<Type, JmModule> Map = new Dictionary<Type, JmModule>();

        public static MethodInBuilder Mib;
        public static MethodOutBuilder2 Mob;

        static JmGlobal()
        {
            var job = JsonObjectBuilder.BuildDefault();
            Mib = new MethodInBuilder(job);
            var jmb = JsonModelBuilder.BuildDefault();
            Mob = new MethodOutBuilder2(jmb);
        }

        public static JmModule Retrieve(Type vt)
        {
            JmModule jmod;
            if (Map.TryGetValue(vt, out jmod))
                return jmod;

            jmod = new JmModule();
            jmod.BuildJmMethodMap(vt);
            Map.Add(vt, jmod);
            return jmod;
        }
    }

    class AspncHttpRequest : IceApiRequest
    {
        HttpRequest _raw;
        Dictionary<string, string> _headers = null;
        Dictionary<string, string> _cookies = null;
        Dictionary<string, string> _query = null;

        public AspncHttpRequest(HttpRequest raw) => _raw = raw;

        public object Raw => _raw;

        public string Url => _raw.Scheme + "://" + _raw.Host + _raw.Path;

        public string Path => _raw.Path;

        public string Method => _raw.Method;

        public string QueryString => _raw.QueryString.Value;

        public string ContentType => _raw.ContentType;

        public long? ContentLength => _raw.ContentLength;

        public IDictionary<string, string> Headers => _GetHeaders();

        public IDictionary<string, string> Cookies => _GetCookies();

        public IDictionary<string, string> Query => _GetQuery();

        IDictionary<string, string> _GetHeaders()
        {
            if (_headers == null)
            {
                _headers = new Dictionary<string, string>();
                foreach (var pair in _raw.Headers)
                    _headers.Add(pair.Key, pair.Value);
            }
            return _headers;
        }

        IDictionary<string, string> _GetCookies()
        {
            if (_cookies == null)
            {
                _cookies = new Dictionary<string, string>();
                foreach (var pair in _raw.Cookies)
                    _cookies.Add(pair.Key, pair.Value);
            }
            return _cookies;
        }

        IDictionary<string, string> _GetQuery()
        {
            if (_query == null)
            {
                _query = new Dictionary<string, string>();
                foreach (var pair in _raw.Query)
                    _query.Add(pair.Key, pair.Value);
            }
            return _query;
        }

        public Stream GetStream() => _raw.Body;
    }

    static class JmWebInvoker
    {
        public delegate IceApiResponse HttpCallDelegate(IceApiRequest request);

        public static IceApiResponse Invoke(JmMethod jm, object instance, HttpRequest request)
        {
            var flags = jm.Attr.Flags;
            if (flags == IceApiFlag.Http)
            {
                var req = new AspncHttpRequest(request);
                var f = (HttpCallDelegate)jm.MI.CreateDelegate(typeof(HttpCallDelegate), instance);
                return f(req);
            }

            var reqBody = ParseJsonRequestBody(request);
            var reqMap = ParseQueryMap(request);
            
            var args = JmGlobal.Mib.PrepareJsonMethodArgs(jm.MI, reqBody, reqMap, request);
            object ret = jm.MI.Invoke(instance, args);
            
            if (flags == IceApiFlag.Json)
            {
                var apir = JmGlobal.Mob.BuildJsonStyleApiReturn(jm, ret);
                return apir;
            }
            else if (flags == IceApiFlag.JsonIn)
            {
                G.Verify(ret != null && ret is IceApiResponse, "WrongRet");
                return (IceApiResponse)ret;
            }
            else
                throw new NutsException("InvalidIceApiFlag", jm.Attr.Flags.ToString());
        }

        #region Parse Request

        static JsonNode EmptyJson = new JsonNode(JsonNodeType.Dictionary);

        static JsonNode ParseJsonRequestBody(HttpRequest request)
        {
            var data = ReadPostBytes(request);
            if (data == null)
                return EmptyJson;

            var text = Encoding.UTF8.GetString(data);
            var jnode = JsonParser.ParseText(text);
            WebGlobal.curEnv.postStr = text;
            return jnode;
        }

        static byte[] ReadPostBytes(HttpRequest request)
        {
            if (request.Body == null)
                return null;

            using (var mstream = new MemoryStream(request.ContentLength > 0 ? (int)request.ContentLength : 0))
            {
                Nano.Net.ResponseReader.CopyStream(request.Body, mstream, new byte[0x10000]);
                if (mstream.Length != 0)
                    return mstream.ToArray();
                else
                    return null;
            }
        }

        static Dictionary<string, string> ParseQueryMap(HttpRequest request)
        {
            var map = new Dictionary<string, string>();
            foreach (var pair in request.Query)
                map.Add(pair.Key, pair.Value);
            foreach (var pair in request.Cookies)
                map[pair.Key] = pair.Value;
            WebGlobal.curEnv.queryStr = JsonModel.Dumps(map);
            return map;
        }
        
        #endregion
    }

    public abstract class JmController : ControllerBase
    {
        JmModule _jmod;

        protected JmController()
        {
            _jmod = JmGlobal.Retrieve(GetType());
        }

        [Route("{_verb}")]
        [HttpGet]
        [HttpPost]
        public IceApiResponse _MethodDispatch()
        {
            const string InternalServerError = "InternalServerError";
            
            IceApiResponse response = null;
            var env = new Env(Request);
            try
            {
                var verb = (string)RouteData.Values["_verb"];
                Console.WriteLine(verb);
                JmMethod jm = _jmod.GetJmMethod(verb);
                if (jm == null)
                {
                    Logger.Err("VerbNotFound", "Url=" + verb);
                    WebGlobal.curEnv.stat = "VerbNotFound";
                    response = IceApiResponse.Error("VerbNotFound", verb);
                }

                response = JmWebInvoker.Invoke(jm, this, Request);
            }
            catch (TargetInvocationException e)
            {
                NutsException inner = e.InnerException as NutsException;
                if (inner != null)
                {
                    Logger.Err(env.reqId + "\t" + "NutsException: " + inner.Code, inner.StackTrace + "\n" + (inner.InnerException != null ? inner.InnerException.StackTrace : ""));
                    WebGlobal.curEnv.stat = inner.Code;
                    response = IceApiResponse.Error(inner.Code, inner.Message);
                    
                }
                else
                {
                    Logger.Err(env.reqId + "\t" + "Exception: " + e.InnerException.Message, e.InnerException.StackTrace + "\n" + (e.InnerException != null ? e.InnerException.StackTrace : ""));
                    string stat = "InternalServerError";
                    string msg = "服务器端异常";
                    WebGlobal.curEnv.stat = stat;
                    response = IceApiResponse.Error(InternalServerError, msg);
                    SentryUtil.Notify(Sentry.Protocol.SentryLevel.Error, e.InnerException, env.reqId + "\t" + "Exception: " + e.InnerException.Message);
                }
            }
            catch (NutsException e)
            {
                Logger.Err(env.reqId + "\t" + "NutsException: " + e.Code, e.StackTrace + "\n" + (e.InnerException != null ? e.InnerException.StackTrace : ""));
                WebGlobal.curEnv.stat = e.Code;
                response = IceApiResponse.Error(e.Code, e.Message);
            }
            catch (Exception e)
            {
                string stat = "InternalServerError";
                string msg = "服务器端异常";
                Logger.Err(env.reqId + "\t" + "Exception: " + e.Message, e.StackTrace + "\n" + (e.InnerException != null ? e.InnerException.StackTrace : ""));
                WebGlobal.curEnv.stat = stat;
                response = IceApiResponse.Error(InternalServerError, msg);
                SentryUtil.Notify(Sentry.Protocol.SentryLevel.Error, e, env.reqId + "\t" + "Exception: " + e.Message);
            }
            _AccessLog(env);
            return response;
        }

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
            sb.Append(env.request.Path);
            sb.Append(SEP);
            sb.Append(env.stat);
            sb.Append(SEP);
            sb.Append(FilterLog.Filter(env.postStr));
            sb.Append(SEP);
            sb.Append(FilterLog.Filter(env.queryStr));
            if (env.logParams != null)
            {
                foreach (var param in env.logParams)
                {
                    sb.Append(SEP);
                    var retJn = DObject.ImportJson(param.Value);
                    var retStr = retJn.ToString();
                    sb.Append(param.Key + ":");
                    sb.Append(FilterLog.Filter(retStr));
                }

            }
            Logger.Acc(null, sb.ToString());
        }
        
    }
    public class Env
    {
        public string ip;
        public string stat;
        public string reqId;
        public long startTime;
        public string postStr;
        public string queryStr;
        public string host;
        public HttpRequest request;
        public Dictionary<string, JsonNode> logParams;
        public Env(HttpRequest request)
        {
            startTime = UnixTimestamp.GetUtcNowTimeValue();
            reqId = Convert.ToBase64String(Guid.NewGuid().ToByteArray(), 0, 12);
            this.request = request;
            this.host = request.Path;
            var ipValue = request.Headers["X-Forwarded-For"];
           
            if (ipValue == StringValues.Empty)
                ip = request.HttpContext.Connection.RemoteIpAddress.ToString();

            //request.HttpContext.Headers.Add("X-ReqId", this.reqId);
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
