using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Nano.Common;
using System.IO;

namespace Nano.Sockets
{
	// Used only in server side
	public abstract class SimpleChannel
	{
		public long Cid = 0; // Channel id
		public long Uid = 0; // mostly user id
		public string Addr = null;
	}

	public interface ISimpleHandler
	{
		void Connected(SimpleChannel channel);
		byte[] Request(byte[] data, int offset, int count, SimpleChannel channel);
		string RequestText(string str, SimpleChannel channel); // invoked by WebSocket
		void Closed(SimpleChannel channel);
	}

	public interface ISimpleServerNotify
	{
		void Notify(List<long> uids, byte[] data, int offset, int count);
		void Notify(List<long> uids, string body);	// differ in WebSocket
	}

	public interface ISimpleServer : ISimpleServerNotify
	{
		void Start(int port);
		void Close();
	}

	static class SimpleSocketPackage
	{
		// package header
		// uint mark
		public const uint MT_Marks = 0xfacecab0;
		public const uint MT_ClientRequest = MT_Marks | 1;
		public const uint MT_ServerResponse = MT_Marks | 2;
		public const uint MT_ServerNotify = MT_Marks | 3;
		// uint sequence
		// uint body length

		public static byte[] BuildPackage(uint mt, uint sq, byte[] data, int offset, int count)
		{
			var tdata = new byte[count + 12];
			ExtConvert.CopyToArray(tdata, 0, mt);
			ExtConvert.CopyToArray(tdata, 4, sq);
			ExtConvert.CopyToArray(tdata, 8, count);
			Array.Copy(data, offset, tdata, 12, count);
			return tdata;
		}

		public static void ReadPackage(byte[] data, out uint mt, out uint sq, out uint offset, out uint count)
		{
			mt = BitConverter.ToUInt32(data, 0);
			Debug.Assert((mt & MT_Marks) == MT_Marks);
			sq = BitConverter.ToUInt32(data, 4);
			count = BitConverter.ToUInt32(data, 8);
			offset = 12;
			Debug.Assert(count + offset == data.Length);
		}
	}

	// Used by SimpleServer & SimpleClient
	class SimpleSocketChannel : SimpleChannel
	{
		public delegate void ReceivedDelegate(uint mt, uint seq, byte[] data, int offset, int count, SimpleSocketChannel channel);
		public ReceivedDelegate Received = null;

		public delegate void ClosedDelegate(SimpleSocketChannel channel);
		public ClosedDelegate Closed = null;

		SocketClient m_socket;

		internal SimpleSocketChannel()
		{
			m_socket = null;
			Cid = Uid = 0;
			Addr = null;
		}

		internal SimpleSocketChannel(SocketClient socket, long cid)
		{
			m_socket = socket;
			m_socket.DataReceived += socket_DataReceived;
			m_socket.Closed += socket_Closed;

			Cid = cid;
			Uid = 0;

			IPEndPoint ep = (IPEndPoint)socket.RawObject.RemoteEndPoint;
			Addr = ep.Address.ToString();
		}

		internal bool Available
		{
			get { return m_socket != null; }
		}

		internal void Connect(string addr, int port)
		{
			m_socket = new SocketClient();
			m_socket.DataReceived += socket_DataReceived;
			m_socket.Closed += socket_Closed;

			try
			{
				var ipaddr = IPAddress.Parse(addr);
				m_socket.Connect(ipaddr, port);
				Addr = addr;
				Thread.Sleep(50);
				// workarounds
				// Sometimes the first request may timed-out if no Sleep here.
				// Steps to repeat the bug easier:
				// 1. Click "Login" while server is not started, then wait for the error message.
				// 2. Start the server
				// 3. Click "Login" again
			}
			catch
			{
				m_socket = null;
				Addr = null;
				throw;
			}
		}

		internal void Send(uint mt, uint sq, byte[] data, int off, int len)
		{
			var tdata = SimpleSocketPackage.BuildPackage(mt, sq, data, off, len);
			m_socket.Send(tdata, 0, tdata.Length);
		}

		internal void Close()
		{
			m_socket.Close();
			m_socket = null;
			Addr = null;
			Cid = Uid = 0;
			Closed?.Invoke(this);
		}

        class UnCompletedCtx
        {
            public MemoryStream buffer = new MemoryStream();
            public int left;

            public uint mt;
            public uint sq;
            public uint offset;
            public uint count;
        }
        private UnCompletedCtx _uncompleted;

		private void socket_DataReceived(SocketClient sock, SocketClient.DataReceiveObject state)
		{
			var data = SegmentsReader.ReadArray(state);

            if (this._uncompleted != null)
            {
                this._uncompleted.left -= data.Length;
                this._uncompleted.buffer.Write(data, 0, data.Length);
                Console.WriteLine("{0} bytes left to read, total:{1}, pos:{2}", this._uncompleted.left, this._uncompleted.count, this._uncompleted.buffer.Position);
                if (this._uncompleted.left < 1)
                {
                    Received?.Invoke(this._uncompleted.mt, this._uncompleted.sq, this._uncompleted.buffer.ToArray(), 
                        (int)this._uncompleted.offset, (int)this._uncompleted.count, this);
                    this._uncompleted = null;
                }
                return;
            }

			uint mt, sq, offset, count;
			SimpleSocketPackage.ReadPackage(data, out mt, out sq, out offset, out count);

            if (offset + count > data.Length)
            {
                this._uncompleted = new UnCompletedCtx()
                {
                    left = (int)(offset + count - data.Length),

                    mt = mt,
                    sq = sq,
                    offset = offset,
                    count = count
                };
                this._uncompleted.buffer.Write(data, 0, data.Length);
                return;
            }

			Received?.Invoke(mt, sq, data, (int)offset, (int)count, this);
		}

		private void socket_Closed(SocketClient sock)
		{
			Console.WriteLine("closed from " + Addr);
			m_socket = null;
			Addr = null;
			Closed?.Invoke(this);
		}
	}

	public class SimpleSocketServer : ISimpleServer
	{
		ISimpleHandler m_handler;
		SocketListener m_listener;
		List<SimpleSocketChannel> m_channels;
		long m_cidSeed;

		public SimpleSocketServer(ISimpleHandler handler)
		{
			m_handler = handler;
			m_listener = new SocketListener();
			m_listener.RequestAccepted += listener_RequestAccepted;
			m_channels = new List<SimpleSocketChannel>();
			m_cidSeed = 0;
		}

		private void listener_RequestAccepted(SocketClient sock)
		{
			IPEndPoint ep = (IPEndPoint)sock.RawObject.RemoteEndPoint;
			Console.WriteLine("connected from " + ep.Address.ToString());
			var channel = new SimpleSocketChannel(sock, ++m_cidSeed);
			channel.Received += channel_Received;
			channel.Closed += channel_Closed;
			m_channels.Add(channel);
			m_handler.Connected(channel);
		}

		public void Start(int port)
		{
			var addr = IPAddress.Parse("0.0.0.0");
			m_listener.Listen(addr, port);
			Console.WriteLine("Socket server listening at port " + port);
		}

		public void Close()
		{
			m_listener.Close();
		}

		void channel_Received(uint mt, uint seq, byte[] data, int offset, int count, SimpleSocketChannel channel)
		{
			Debug.Assert(mt == SimpleSocketPackage.MT_ClientRequest);
			// data = InvokeRequest(mt, seq, data, offset, count, channel);
			data = m_handler.Request(data, offset, count, channel);
			channel.Send(SimpleSocketPackage.MT_ServerResponse, seq, data, 0, data.Length);
		}

		void channel_Closed(SimpleSocketChannel channel)
		{
			m_handler.Closed(channel);
			m_channels.Remove(channel);
		}

		public void Notify(List<long> uids, byte[] data, int offset, int count)
		{
			Dictionary<long, int> idmap = new Dictionary<long, int>();
			foreach (var uid in uids)
				idmap.Add(uid, 0);

			foreach (var channel in m_channels)
			{
				if (idmap.ContainsKey(channel.Uid))
				{
					++idmap[channel.Uid];
					channel.Send(SimpleSocketPackage.MT_ServerNotify, 0, data, offset, count);
				}
			}

			uids.RemoveAll(x => idmap[x] != 0);
		}

		public void Notify(List<long> uids, string body)
		{
			var data = Encoding.UTF8.GetBytes(body);
			Notify(uids, data, 0, data.Length);
		}
	}

	public class SimpleSocketClient
	{
		class WaitObject
		{
			public AutoResetEvent Event = new AutoResetEvent(false);
			public byte[] Data = null;
		}

		public delegate void ServerNotifyDelegate(byte[] data);
		public ServerNotifyDelegate ServerNotify = null;

		SimpleSocketChannel m_channel = null;
		string m_addr = null;
		int m_port = 0;
		uint m_sq = 0;
		Dictionary<uint, WaitObject> m_waits = new Dictionary<uint, WaitObject>();

		public void Init(string addr, int port)
		{
			m_channel = new SimpleSocketChannel();
			m_channel.Received += channel_Received;
			m_addr = addr;
			m_port = port;
		}

		void ValidateChannel()
		{
			if (!m_channel.Available)
				m_channel.Connect(m_addr, m_port);
			Debug.Assert(m_channel.Available);
		}

		public byte[] Request(byte[] data, int off, int len)
		{
			// Make wait object
			const int timeout = 30 * 1000;
			uint sq = ++m_sq;
			var wo = new WaitObject();
			m_waits.Add(sq, wo);

			// Send request package
			ValidateChannel();
			m_channel.Send(SimpleSocketPackage.MT_ClientRequest, sq, data, off, len);

			// Wait for response
			bool f = wo.Event.WaitOne(timeout);
			m_waits.Remove(sq);
			if (!f)
				throw new Exception("Request timeout");
			return wo.Data;
		}

		public void Close()
		{
			m_channel.Close();
			m_channel = null;
		}

		void channel_Received(uint mt, uint seq, byte[] data, int offset, int count, SimpleSocketChannel channel)
		{
			if (mt == SimpleSocketPackage.MT_ServerResponse)
			{
				WaitObject wo;
				if (m_waits.TryGetValue(seq, out wo))
				{
					wo.Data = new byte[count];
					Array.Copy(data, offset, wo.Data, 0, count);
					wo.Event.Set();
				}
			}
			else if (mt == SimpleSocketPackage.MT_ServerNotify)
			{
				var tdata = new byte[count];
				Array.Copy(data, offset, tdata, 0, count);
				ServerNotify?.Invoke(tdata);
			}
			else
				throw new NotSupportedException("Unsupported MT=" + (mt & 0xF));
		}
	}
}
