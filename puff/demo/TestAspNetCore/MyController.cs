using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nano.Nuts;
using Puff.NetCore;

namespace TestAspNetCore
{
    [Route("[controller]")]
    [ApiController]
    public class MyController : JmController
    {
        [IceApi()]
        void Ping() { }

        [IceApi()]
        object Echo(string name, int count) => new { name, count };

        [IceApi()]
        (int x, int y) Swap(int x, int y) => (y, x);

        [IceApi()]
        void Throw(string code, string m = "") => throw new NutsException(code, m);

        [IceApi(Ret = "value")]
        int Div(int x, int y) => x / y;

        [IceApi(Ret = "stat")]
        string Stat(string stat) => stat;

        [IceApi(Stat = null)]
        void NoStat() { }

        [IceApi(Cookie = "name,value")]
        (string name, int value) Cookie(string name, int value) => ("x-" + name, value + 1);

        [IceApi(Flags = IceApiFlag.JsonIn)]
        IceApiResponse SayHello(string name) => IceApiResponse.String($"Hello, {name}!");

        [IceApi(Flags = IceApiFlag.JsonIn)]
        IceApiResponse SaveHello(string name)
        {
            var r = IceApiResponse.String($"Hello, {name}!");
            r.SetToSave(name + ".txt");
            return r;
        }

        [IceApi(Flags = IceApiFlag.Http)]
        IceApiResponse Http(IceApiRequest request)
        {
            var oHeader = DObject.New(request.Headers);
            var oCookie = DObject.New(request.Cookies);
            var oQuery = DObject.New(request.Query);
            var body = GetBodyText(request);
            DObject o = new DObject.DMap
            {
                { "url", request.Url }, { "path", request.Path }, { "method", request.Method },
                { "qs", request.QueryString }, { "ctype", request.ContentType }, { "clen", request.ContentLength },
                { "header", oHeader }, { "cookie", oCookie }, { "query", oQuery },
                { "body", body }
            };
            var response = new IceApiResponse {
                HttpStatusCode = 200, Json = DObject.ExportJson(o),
                Cookies = (Dictionary<string, string>)request.Cookies
            };
            if (request.Headers.ContainsKey("Secret"))
                response.Headers = new Dictionary<string, string> { { "Secret", "x-" + request.Headers["Secret"] } };
            return response;
        }

        static string GetBodyText(IceApiRequest request)
        {
            var istream = request.GetStream();
            if (istream == null)
                return null;

            var mstream = new MemoryStream();
            Nano.Net.ResponseReader.CopyStream(istream, mstream, new byte[0x10000]);
            var data = mstream.ToArray();
            return Encoding.UTF8.GetString(data);
        }
    }

    public static class WebSocketAdapter
    {
        public static async Task WebSocketFilter(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var ws = await context.WebSockets.AcceptWebSocketAsync();
                    await WebSocketAdapter.Echo(context, ws);
                }
                else
                    context.Response.StatusCode = 400;
            }
            else
                await next();
        }

        public static async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
