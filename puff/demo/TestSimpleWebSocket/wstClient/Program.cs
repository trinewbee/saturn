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
	static long m_seq = 0;

	static async Task ReceiveMessagesAsync(ClientWebSocket client, CancellationToken token)
	{
		byte[] buffer = new byte[0x100000];
		while (client.State == WebSocketState.Open)
		{
			try
			{
				WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					Console.WriteLine("Server requested close. Closing connection...");
					await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed connection", token);
				}
				else
				{
					string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
					Console.WriteLine($"RESP {receivedMessage}\r\n");
				}
			}
			catch (WebSocketException e)
			{
				Console.WriteLine($"WebSocket receive error: {e.Message}");
				break;
			}
		}
	}

	static async Task Invoke(ClientWebSocket ws, string url, Dictionary<string, object> args, long uid = 0)
	{
		args.Add("sc:m", url);
		args.Add("sc:q", ++m_seq);
		if (uid != 0)
			args.Add("sc:id", uid);

		var o = DObject.New(args);
		var str = DObject.ExportJsonStr(o);
		Console.WriteLine("REQ " + str);
		await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(str)), WebSocketMessageType.Text, true, CancellationToken.None);

		if (false)
		{
			var buffer = new byte[0x100000];
			var r = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			Debug.Assert(r.EndOfMessage);
			var str_t = Encoding.UTF8.GetString(buffer, 0, r.Count);
			Console.WriteLine("RESP " + str_t + "\r\n");
		}
	}

	static async Task TestWebSockets()
	{
		var ws = new ClientWebSocket();
		var uri = new Uri("ws://127.0.0.1:10002");
		await ws.ConnectAsync(uri, CancellationToken.None);

		ReceiveMessagesAsync(ws, CancellationToken.None);

		while (true)
		{
			Console.Write(": ");
			var line = Console.ReadLine().Trim();
			if (line.Length == 0)
				continue;
			else if (line == "exit" || line == "quit")
				break;
			else if (line == "ping")
				await Invoke(ws, "/api/ping", new());
			else if (line == "hello")
				await Invoke(ws, "/api/hello", new() { ["name"] = "Mandy", ["age"] = 12 });
			else if (line == "say")
				await Invoke(ws, "/api/say", new(), uid: 3);
			else
				Console.WriteLine("Unknown command\r\n");
		}

		await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
	}

	static void Main(string[] args)
	{
		var task = TestWebSockets();
		task.Wait();
	}
}
