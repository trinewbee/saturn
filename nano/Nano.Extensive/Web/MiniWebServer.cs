using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace Nano.Ext.Web
{
	public static class RequestReader
	{
		public static void ReadPost(HttpListenerRequest request, Stream ostream)
		{
			using (var istream = request.InputStream)
				Nano.Net.ResponseReader.CopyStream(istream, ostream, new byte[65536]);
		}

		public static MemoryStream GetPost(HttpListenerRequest request)
		{
			long length = request.ContentLength64;
			if (length >= 0x80000000)
				throw new ArgumentOutOfRangeException();
			
			var ostream = new MemoryStream((int)length);
			ReadPost(request, ostream);
			return ostream;
		}

		public static byte[] GetPostBytes(HttpListenerRequest request) => GetPost(request).ToArray();

		public static string GetPostText(HttpListenerRequest request)
		{
			byte[] data = GetPostBytes(request);
			return Encoding.UTF8.GetString(data);
		}
	}

	public static class ResponseWriter
	{
		public static void SendResponse(HttpListenerResponse response, string text, int status = 200)
		{
			var data = Encoding.UTF8.GetBytes(text);
			SendResponse(response, data, 0, data.Length, status);
		}

		public static void SendResponse(HttpListenerResponse response, byte[] data, int offset, int count, int status = 200)
		{
			response.StatusCode = status;
			response.ContentLength64 = count;
			using (var ostream = response.OutputStream)
				ostream.Write(data, offset, count);
		}

		public static void SendResponse(HttpListenerResponse response, Stream istream, int status = 200)
		{
			response.StatusCode = status;
			response.ContentLength64 = istream.Length;
			istream.Seek(0, SeekOrigin.Begin);
			using (var ostream = response.OutputStream)
				Nano.Net.ResponseReader.CopyStream(istream, ostream, new byte[65536]);
		}
	}

	public interface IUrlDispatch
	{
		void Dispatch(string url, HttpListenerContext ctx);
	}

	public class MiniUrlDispatch : IUrlDispatch
	{
		public delegate void Invoke(string url, HttpListenerContext ctx);

		public Dictionary<string, Invoke> Urls = new Dictionary<string, Invoke>();
		public Invoke Default = InnerDefault;

		public void Dispatch(string url, HttpListenerContext ctx)
		{
			Invoke f;
			if (Urls.TryGetValue(url, out f))
				f(url, ctx);
			else if (Default != null)
				Default(url, ctx);
		}

		static void InnerDefault(string url, HttpListenerContext ctx)
		{
			const string text = "Mini Server";
			ResponseWriter.SendResponse(ctx.Response, text);
		}
	}

	public class MiniServer
	{
		IUrlDispatch m_dispatch;
		HttpListener m_listener = null;

		public MiniServer(IUrlDispatch dispatch)
		{
			m_dispatch = dispatch;
		}

		public void Start(uint port)
		{
			Debug.Assert(m_listener == null);
			m_listener = new HttpListener();
			m_listener.Prefixes.Add(string.Format("http://+:{0}/", port));
			m_listener.Start(); // netsh http add urlacl url=http://+:8080/ user=Everyone
			m_listener.BeginGetContext(new AsyncCallback(GetContextCallBack), this);
			Console.WriteLine("Starting web server at port {0}", port);
		}

		public void Close()
		{
			m_listener.Close();
		}

		static void GetContextCallBack(IAsyncResult ar)
		{
			try
			{
				MiniServer server = (MiniServer)ar.AsyncState;
				HttpListener listener = server.m_listener;
				HttpListenerContext ctx = listener.EndGetContext(ar);
				listener.BeginGetContext(new AsyncCallback(GetContextCallBack), server);
				string url = ctx.Request.Url.LocalPath;
				server.m_dispatch.Dispatch(url, ctx);
			}
			catch
			{
			}
		}
	}
}
