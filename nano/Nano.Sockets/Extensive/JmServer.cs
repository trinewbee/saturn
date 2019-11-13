using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;

namespace Nano.Sockets
{
	public interface JmService
	{
		JsonNode Invoke(JsonNode jnode, SimpleChannel channel);
		void Close(SimpleChannel channel);
	}

	public interface JmServiceNotify
	{
		void Notify(List<long> userIds, JsonNode jnode);
	}

	class JmServiceHandler : ISimpleHandler
	{
		JmService m_service = null;

		public JmServiceHandler(JmService service) { m_service = service; }

		public void Connected(SimpleChannel channel) { }

		public byte[] Request(byte[] data, int offset, int count, SimpleChannel channel)
		{
			string str = Encoding.UTF8.GetString(data, offset, count);
			str = RequestText(str, channel);
			return Encoding.UTF8.GetBytes(str);
		}

		public string RequestText(string str, SimpleChannel channel)
		{
			Console.WriteLine("receive (" + channel.Cid + ", " + channel.Uid + ", " + channel.Addr + "), body=" + str);
			JsonNode jnode = JsonParser.ParseText(str);
			jnode = m_service.Invoke(jnode, channel);

			var jw = new JsonWriter(jnode);
			str = jw.GetString();
			Console.WriteLine("send (" + channel.Cid + ", " + channel.Uid + ", " + channel.Addr + "), body=" + str);
			return str;
		}

		public void Closed(SimpleChannel channel) => m_service.Close(channel);
	}

	public class JmServer : JmServiceNotify
	{
		SimpleSocketServer m_socksvr = null;
		SimpleWsServer m_wssvr = null;

		ISimpleServer m_simple = null;

		public void Start(JmService service, int portSockets, int portWebSock)
		{
			var handler = new JmServiceHandler(service);
			if (portSockets != 0)
			{
				m_socksvr = new SimpleSocketServer(handler);
				m_socksvr.Start(portSockets);
			}
			if (portWebSock != 0)
			{
				m_wssvr = new SimpleWsServer(handler);
				m_wssvr.Start(portWebSock);
			}
		}

		public void Notify(List<long> uids, JsonNode jnode)
		{
			var jw = new JsonWriter(jnode);
			var str = jw.GetString();
			Console.WriteLine("notify body=" + str);

			var data = Encoding.UTF8.GetBytes(str);
			m_socksvr?.Notify(uids, data, 0, data.Length);
			// m_wssvr?.Notify(uids, data, 0, data.Length);
			m_wssvr?.Notify(uids, str);
		}

		public void Close()
		{
			m_socksvr?.Close();
			m_wssvr?.Close();
		}
	}
}
