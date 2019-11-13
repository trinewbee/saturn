using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Nano.Sockets
{
	class SimpleWsChannel : SimpleChannel
	{
		internal WebSocket m_wsock;

		internal void Send(byte[] data, int offset, int count)
		{
			if (offset != 0 || count != data.Length)
			{
				var tdata = new byte[count];
				Array.Copy(data, offset, tdata, 0, count);
				data = tdata;
			}

			m_wsock.Send(data);
		}

		internal void Send(string body)
		{
			m_wsock.Send(body);
		}
	}

	class SimpleWsBehavior : WebSocketBehavior
	{
		SimpleWsServer m_serv;
		ISimpleHandler m_handler;
		SimpleWsChannel m_channel;

		public SimpleWsBehavior(SimpleWsServer serv)
		{
			m_serv = serv;
			m_handler = serv.Handler;
			m_channel = null;
		}

		protected override void OnOpen()
		{
			var ws = Context.WebSocket;
			m_channel = m_serv.CreateChannel(ws);
			m_handler.Connected(m_channel);
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			Debug.Assert(m_channel != null && Context.WebSocket == m_channel.m_wsock);
			if (e.IsText)
			{
				var str = m_handler.RequestText(e.Data, m_channel);
				this.Send(str);
			}
			else
			{
				byte[] data = e.RawData;
				data = m_handler.Request(data, 0, data.Length, m_channel);
				this.Send(data);
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			m_handler.Closed(m_channel);
			m_serv.DeleteChannel(m_channel);
			m_channel = null;
		}
	}

	public class SimpleWsServer : ISimpleServer
	{
		ISimpleHandler m_handler;
		WebSocketServer m_wssv;
		List<SimpleWsChannel> m_channels;
		long m_cidSeed;

		public SimpleWsServer(ISimpleHandler handler)
		{
			m_handler = handler;
			m_wssv = null;
			m_channels = new List<SimpleWsChannel>();
			m_cidSeed = 0;
		}

		internal ISimpleHandler Handler
		{
			get { return m_handler; }
		}

		public void Start(int port)
		{
			m_wssv = new WebSocketServer("ws://0.0.0.0:" + port);
			// string address = m_wssv.Address.ToString();
			// int port = m_wssv.Port;

			m_wssv.AddWebSocketService<SimpleWsBehavior>("/", () => new SimpleWsBehavior(this));
			m_wssv.Start();
			Console.WriteLine("WebSocket server listening at port " + port);
		}

		public void Close()
		{
			m_wssv.Stop();
			m_wssv = null;
		}

		public void Notify(List<long> uids, byte[] data, int offset, int count)
		{
			NotifyImpl(uids, channel => channel.Send(data, offset, count));
		}
		
		public void Notify(List<long> uids, string body)
		{
			NotifyImpl(uids, channel => channel.Send(body));
		}

		void NotifyImpl(List<long> uids, Action<SimpleWsChannel> action)
		{
			Dictionary<long, int> idmap = new Dictionary<long, int>();
			foreach (var uid in uids)
				idmap.Add(uid, 0);

			foreach (var channel in m_channels)
			{
				if (idmap.ContainsKey(channel.Uid))
				{
					++idmap[channel.Uid];
					action(channel);
				}
			}

			uids.RemoveAll(x => idmap[x] != 0);
		}

		internal SimpleWsChannel CreateChannel(WebSocket wsock)
		{
			var channel = new SimpleWsChannel() { Cid = ++m_cidSeed, Uid = 0, m_wsock = wsock };
			m_channels.Add(channel);
			return channel;
		}

		internal void DeleteChannel(SimpleWsChannel channel)
		{
			m_channels.Remove(channel);
		}
	}
}
