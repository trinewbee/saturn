using System;
using System.Collections.Generic;
using Nano.Logs;
using Puff.Model;
using Puff.Servers;

[IceService(BaseUrl = "/api")]
class TestApi
{
	public delegate void EchoDelegate(List<long> uids);

	[IceNotify()]
	public EchoDelegate Echo;

	[IceApi()]
	public void Ping() { }

	[IceApi(Ret = "m")]
	public string Hello(string name, int age) => $"My name is {name}. I'm {age} yrs old.";

	[IceApi()]
	public void Say() => Echo(new() { 3 });
}

static class WebSocketTestServerApp
{
	static void StartServer()
	{
		uint portWeb = 10001, portWebSock = 10002;
		var smb = new PuffServer();
		try
		{
			smb.AddService(new TestApi());
			smb.StartServer(portWeb: portWeb, portWebSock: portWebSock);
			Console.WriteLine("Press enter key");
			Console.ReadLine();
			smb.StopServer();
		}
		catch (System.Net.HttpListenerException e)
		{
			Console.WriteLine("Failed to start HTTP server: " + e.Message);
			Console.WriteLine("Try run the following command as administrator, then restart this application");
			Console.WriteLine($"netsh http add urlacl url=http://+:{portWeb}/ user=Everyone");
		}
	}

	static void Main(string[] args)
	{
		Logger.Init("logs", null, false, 69206016);
		StartServer();
	}
}