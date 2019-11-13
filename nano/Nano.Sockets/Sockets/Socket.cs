using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace Nano.Sockets
{
	/// <summary>用于TCP连接的实例</summary>
	public class SocketClient
	{
		#region Events

		public delegate void DlgConnected(SocketClient sock);
		public delegate void DlgDataSent(SocketClient sock, int cbSent);
		public delegate void DlgDataReceived(SocketClient sock, DataReceiveObject state);
		public delegate void DlgClosed(SocketClient sock);

		/// <summary>连接建立事件</summary>
		/// <remarks>当连接建立时调用此方法。如果是服务端接受连接请求并创建的实例，此方法不会被调用。</remarks>
		public event DlgConnected Connected;

		/// <summary>数据发送事件</summary>
		/// <remarks>当数据发送完成后触发此事件。</remarks>
		public event DlgDataSent DataSent;

		/// <summary>数据接收事件</summary>
		/// <remarks>当接收到数据时触发此事件。</remarks>
		public event DlgDataReceived DataReceived;

		/// <summary>连接断开事件</summary>
		/// <remarks>当连接断开时（主动断开或者对方断开）触发此事件。</remarks>
		public event DlgClosed Closed;

		#endregion

		public class DataReceiveObject
		{
			public SocketClient Sock;
			public byte[] DataFirst;
			public List<byte[]> DataNext = null;
			public int Length = 0;

			public DataReceiveObject(SocketClient _sock, int _rsvBuffer)
			{
				Sock = _sock;
				DataFirst = new byte[_rsvBuffer];
			}
		}

		Socket m_sock;
		public object Tag;
		int m_bufReserved = 65536;

		public SocketClient()
		{
			m_sock = null;
		}

		internal SocketClient(Socket sock)
		{
			m_sock = sock;
			PrepareComm();
		}

		#region Properties

		/// <summary>获取原生对象</summary>
		/// <remarks>获取本实例使用的System.Net.Sockets.Socket对象</remarks>
		public Socket RawObject
		{
			get { return m_sock; }
		}

		/// <summary>获取连接是否活动</summary>
		/// <remarks>当连接建立且没有中断时，此属性为true</remarks>
		public bool Active
		{
			get { return m_sock != null; }
		}

		#endregion

		#region Connect

		internal void PrepareComm()
		{
			PrepareReceive();
		}

		public void AsyncConnect(IPAddress addr, int port)
		{
			Debug.Assert(m_sock == null);
			m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_sock.BeginConnect(addr, port,
				new AsyncCallback(ConnectCallBack), m_sock);
		}

		public void Connect(IPAddress addr, int port)
		{
			Debug.Assert(m_sock == null);
			m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_sock.Connect(addr, port);
			PrepareComm();
		}

		void ConnectCallBack(IAsyncResult ar)
		{
			Socket client = (Socket)ar.AsyncState;
			client.EndConnect(ar);

			if (Connected != null)
				Connected(this);

			PrepareComm();
		}

		#endregion

		public void Close()
		{
			if (m_sock != null)
			{
				m_sock.Shutdown(SocketShutdown.Both);

				// 等待 ReceiveCallBack 来结束
			}
		}

		#region Send

		public void Send(byte[] data, int offset, int length)
		{
			m_sock.Send(data, offset, length, SocketFlags.None);
		}

		public void AsyncSend(byte[] data, int offset, int length)
		{
			m_sock.BeginSend(data, offset, length, SocketFlags.None,
				new AsyncCallback(SendCallBack), m_sock);
		}

		void SendCallBack(IAsyncResult ar)
		{
			Socket client = (Socket)ar.AsyncState;
			try
			{
				int bytesSent = client.EndSend(ar);

				if (DataSent != null)
					DataSent(this, bytesSent);
			}
			catch (SocketException e)
			{
				if (Closed != null)
					Closed(this);

				m_sock.Close();
				m_sock = null;
			}
			catch (ObjectDisposedException e)
			{
				if (Closed != null)
					Closed(this);

				m_sock = null;
			}
		}

		#endregion

		#region Receive

		void PrepareReceive()
		{
			DataReceiveObject state = new DataReceiveObject(this, m_bufReserved);
			m_sock.BeginReceive(state.DataFirst, 0, state.DataFirst.Length, SocketFlags.None,
				new AsyncCallback(ReceiveCallBack), state);
		}

		void ReceiveCallBack(IAsyncResult ar)
		{
			DataReceiveObject state = (DataReceiveObject)ar.AsyncState;
			Debug.Assert(state.Sock == this);

			try
			{
				state.Length = m_sock.EndReceive(ar);
				if (state.Length > 0)
				{
					int dataRemain;
					while ((dataRemain = m_sock.Available) > 0)
					{
						if (state.DataNext == null)
							state.DataNext = new List<byte[]>(4);

						byte[] bufNew = new byte[dataRemain];
						int bytesMore = m_sock.Receive(bufNew, 0, dataRemain, SocketFlags.None);
						Debug.Assert(bytesMore == dataRemain);

						state.DataNext.Add(bufNew);
						state.Length += bytesMore;
					}

					if (DataReceived != null)
						DataReceived(this, state);

					PrepareReceive();
				}
				else
				{
					if (Closed != null)
						Closed(this);

					m_sock.Close();
					m_sock = null;
				}
			}
			catch (SocketException e)
			{
				if (Closed != null)
					Closed(this);

				m_sock.Close();
				m_sock = null;
			}
			catch (ObjectDisposedException e)
			{
				if (Closed != null)
					Closed(this);

				m_sock = null;
			}
		}

		#endregion
	}

	public class SocketListener
	{
		public delegate void DlgRequestAccepted(SocketClient sock);
		public event DlgRequestAccepted RequestAccepted;

		Socket m_sock = null;
		Thread m_thread = null;
		AutoResetEvent m_event = null;
		volatile bool m_fActive = false;
		IPEndPoint m_ep = null;

		public object Tag;

		public bool Active
		{
			get { return m_fActive; }
		}

		public void Listen(IPAddress addr, int port)
		{
			m_ep = new IPEndPoint(addr, port);

			m_event = new AutoResetEvent(false);
			m_fActive = true;

			m_thread = new Thread(new ThreadStart(this.ThreadProc));
			m_thread.Start();
		}

		public void Close()
		{
			if (m_thread != null)
			{
				m_fActive = false;
				m_event.Set();

				m_thread.Join();
				m_thread = null;

				m_event.Close();
				m_event = null;
			}
		}

		void ThreadProc()
		{
			Debug.Assert(m_sock == null);
			m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_sock.Bind(m_ep);
			m_sock.Listen(1000);

			while (m_fActive)
			{
				m_sock.BeginAccept(new AsyncCallback(AcceptCallBack), m_sock);
				m_event.WaitOne();
			}

			// 由于不能 Cancel 最后一次 BeginAccept，因此关闭 Socket 会抛异常
			// 如果不关闭，则端口不会被释放
			// 下面是一个非常山寨的方法，但有效
			/*			Socket sockFake = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sockFake.Connect(m_ep);
                        sockFake.Close();

                        m_event.WaitOne();	// 必须要，否则 EndAccept 会晚于 Close
            */
			m_sock.Close();
			m_sock = null;
		}

		void AcceptCallBack(IAsyncResult ar)
		{
			if (!m_fActive)
				return;

			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			// 为了关闭设置的特殊连接
			if (!m_fActive)
			{
				handler.Close();
				m_event.Set();
				return;
			}

			m_sock.BeginAccept(new AsyncCallback(AcceptCallBack), listener);

			SocketClient sockAccpt = new SocketClient(handler);
			RequestAccepted?.Invoke(sockAccpt);

			m_event.Set();
		}
	}

	public static class SegmentsReader
	{
		public static MemoryStream ReadStream(SocketClient.DataReceiveObject state)
		{
			var stream = new MemoryStream(state.Length);
			stream.Write(state.DataFirst, 0, Math.Min(state.Length, state.DataFirst.Length));
			if (state.DataNext != null)
			{
				Debug.Assert(state.Length > state.DataFirst.Length);
				foreach (var data in state.DataNext)
					stream.Write(data, 0, data.Length);
			}
			else
				Debug.Assert(state.Length <= state.DataFirst.Length);
			Debug.Assert(stream.Length == state.Length);
			return stream;
		}

		public static byte[] ReadArray(SocketClient.DataReceiveObject state)
		{
			var stream = ReadStream(state);
			return stream.ToArray();
		}
	}
}
