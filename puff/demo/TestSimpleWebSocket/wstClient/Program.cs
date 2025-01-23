using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nano.Nuts;

class WebSocketTestClientApp
{
	static async Task<string> Invoke(ClientWebSocket ws, string url, Dictionary<string, object> args)
	{
		args.Add("sc:m", url);
		args.Add("sc:q", 3);
		var o = DObject.New(args);
		var str = DObject.ExportJsonStr(o);
		await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(str)), WebSocketMessageType.Text, true, CancellationToken.None);

		var buffer = new byte[0x100000];
		var r = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		Debug.Assert(r.EndOfMessage);
		var str_t = Encoding.UTF8.GetString(buffer, 0, r.Count);
		return str_t;
	}

	static async Task TestWebSockets()
	{
		var ws = new ClientWebSocket();
		var uri = new Uri("ws://127.0.0.1:10002");
		await ws.ConnectAsync(uri, CancellationToken.None);

		var str = await Invoke(ws, "/api/ping", new());
		Console.WriteLine(str);

		str = await Invoke(ws, "/api/hello", new() { ["name"] = "Mandy", ["age"] = 12 });
		Console.WriteLine(str);

		await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
	}

	static void Main(string[] args)
	{
		var task = TestWebSockets();
		task.Wait();
	}
}
