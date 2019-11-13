using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nano.UnitTest;

namespace TestAspNetCoreClient
{
    class Program
    {
        static async Task TestWebSockets()
        {
            var ws = new ClientWebSocket();
            var uri = new Uri("ws://127.0.0.1:5000/ws");
            await ws.ConnectAsync(uri, CancellationToken.None);

            var str = "Hello, world";
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(str)), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[0x100000];
            var r = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Test.Assert(r.EndOfMessage);
            var str_t = Encoding.UTF8.GetString(buffer, 0, r.Count);
            Test.Assert(str_t == str);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
        }

        static void Main(string[] args)
        {
            var task = TestWebSockets();
            task.Wait();
        }
    }
}
