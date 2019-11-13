using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Collection;
using Nano.Storage;
using Nano.Json;
using Nano.Ext.Marshal;

namespace Nano.Ext.Persist
{
	/// <summary>接收 Binlog 的接收器</summary>
	/// <remarks>
	/// BinlogAccept 仅在 BinlogStore 方法完全打开数据库之后创建，请在 Open 方法完成后使用 ChangeAccept 获取。
	/// </remarks>
	public class BinlogAccept
	{
        const int Minute = 1000 * 60;

        JsonModelSaver m_saver;
        System.Threading.Timer m_timer;

		internal BinlogAccept(JsonModelSaver saver, int elapse = Minute)
		{
			m_saver = saver;
            m_timer = new System.Threading.Timer(timerCallback, null, elapse, elapse);
		}

		/// <summary>写入一个对象</summary>
		/// <param name="o">待写入对象</param>
		/// <remarks>传入的对象会被使用 JsonModel.Dump 转换为 JsonNode</remarks>
		public void WriteObject(object o)
		{
			var jnode = JsonModel.Dump(o);
			WriteJsonNode(jnode);
		}

		/// <summary>写入一个 JSON 对象</summary>
		/// <param name="jnode">待写入对象</param>
		public void WriteJsonNode(JsonNode jnode) => m_saver.Write(jnode);

		/// <summary>关闭本对象</summary>
		public void Close()
		{
            m_timer?.Dispose();
            m_timer = null;
			m_saver?.Close();
			m_saver = null;
		}

        void timerCallback(object state) => m_saver?.Flush();
    }

	/// <summary>提供给 BinlogStore 的回调接口</summary>
	public interface BinlogAccess
	{
		/// <summary>开始载入数据</summary>
		void LoadStarted();

		/// <summary>没有任何数据，将数据初始化</summary>
		void MakeNew();

		/// <summary>载入给定的 MAP 文件</summary>
		void LoadMap(Stream stream);

		/// <summary>载入给定的 LOG 文件</summary>
		void LoadLog(JsonModelLoader jld, Stream stream);

		/// <summary>完成数据载入</summary>
		void LoadCompleted();

		/// <summary>写入 MAP 文件</summary>
		void SaveMap(Stream stream);
	}

	/// <summary>Binlog库</summary>
	public class BinlogStore
	{
		const string SchemaName = "schema.xml";
		const string LogsName = "logs";

		BinlogAccess m_accs;
		FileTreeItem m_root;
		BinlogAccept m_acpt;

		/// <summary>初始化一个BinlogStore</summary>
		/// <param name="accs">回调接口</param>
		/// <param name="root">保存数据的目录对象</param>
		public BinlogStore(BinlogAccess accs, FileTreeItem root)
		{
			m_accs = accs;
			m_root = root;
			m_acpt = null;
		}

		/// <summary>初始化一个BinlogStore</summary>
		/// <param name="accs">回调接口</param>
		/// <param name="path">保存数据的路径</param>
		public BinlogStore(BinlogAccess accs, string path)
		{
			if (!Directory.Exists(path))
				throw new IOException("Directory not found, " + path);
			var fta = new LocalFileTreeAccess(path);
			m_accs = accs;
			m_root = fta.Root;
			m_acpt = null;
		}

		/// <summary>获取接受变更的 BinlogAccept</summary>
		/// <remarks>仅当 Open 流程全部完成后，才能获取该对象。</remarks>
		public BinlogAccept ChangeAccept
		{
			get { return m_acpt; }
		}

		/// <summary>创建一个 JSON 保存器</summary>
		/// <param name="stream">用于写入数据的流</param>
		/// <returns>返回创建的保存器</returns>
		public JsonModelSaver CreateJsonSaver(Stream stream) => new StringJsonModelSaver(stream);

		/// <summary>创建一个 JSON 载入器</summary>
		/// <param name="stream">用于读取数据的流</param>
		/// <returns>返回创建的载入器</returns>
		public JsonModelLoader CreateJsonLoader() => new StringJsonModelLoader();

		/// <summary>打开数据库</summary>
		public void Open()
		{
			Console.WriteLine("Open database");
			Load();
			CreateCommandAccepter();
		}

		void Load()
		{
			m_accs.LoadStarted();
			var fis = m_root.List();

			long ts = 0;
			var fiMap = SelectLastItem(fis, ".map");
			if (fiMap != null)
			{
				ts = GetNameNumber(fiMap.Name);
				Console.WriteLine("Loading " + fiMap.Name);
				using (var istream = fiMap.Open(false))
					m_accs.LoadMap(istream);
			}
			else
				m_accs.MakeNew();

			var fiLog = SelectLastItem(fis, ".log");
			if (fiLog != null)
			{
				long tsn = GetNameNumber(fiLog.Name);
				if (tsn <= ts)
					throw new Exception("WrongBinlogTime");
				ts = tsn;
				Console.WriteLine("Loading " + fiLog.Name);
				using (var istream = fiLog.Open(false))
				{
					var jld = CreateJsonLoader();
					m_accs.LoadLog(jld, istream);
				}
			}

			m_accs.LoadCompleted();
			WaitAfter(ts);

			if (fiLog != null)
			{
				ts = SaveData();
				MoveOldFiles(fis);
			}
			WaitAfter(ts);
		}

		static long GetNameNumber(string name)
		{
			int pos = name.IndexOf('.');
			if (pos >= 0)
				name = name.Substring(0, pos);
			return long.Parse(name);
		}

		static FileTreeItem SelectLastItem(List<FileTreeItem> fis, string suffix)
		{
			fis = CollectionKit.Select(fis, x => x.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
			if (fis.Count == 0)
				return null;

			fis.Sort(delegate (FileTreeItem x, FileTreeItem y)
			{
				long xt = GetNameNumber(x.Name);
				long yt = GetNameNumber(y.Name);
				return -xt.CompareTo(yt);
			});
			return fis[0];
		}

		static long WaitAfter(long ts)
		{
			while (true)
			{
				long tsn = Nano.Ext.Marshal.UnixTimestamp.GetUtcNowTimeValue();
				if (tsn > ts)
					return tsn;
			}
		}

		long SaveData()
		{
			long ts = Nano.Ext.Marshal.UnixTimestamp.GetUtcNowTimeValue();
			var f = m_root.CreateChild(ts.ToString() + ".map", 0);
			var ostream = f.Item2;
			m_accs.SaveMap(ostream);
			ostream.Close();
			return ts;
		}

		void CreateCommandAccepter()
		{
			long ts = Nano.Ext.Marshal.UnixTimestamp.GetUtcNowTimeValue();
			var f = m_root.CreateChild(ts.ToString() + ".log", 0);

			Debug.Assert(m_acpt == null);
			var saver = CreateJsonSaver(f.Item2);
			m_acpt = new BinlogAccept(saver);
		}

		/// <summary>关闭数据库</summary>
		/// <param name="interrupt">是否强行中断</param>
		/// <remarks>
		/// interrupt 参数仅供测试代码使用，指示强行中断数据库并不产生 MAP 文件。
		/// </remarks>
		public void Close(bool interrupt = false)
		{
			m_acpt.Close();
			m_acpt = null;

			if (interrupt)
			{
				Console.WriteLine("Interrupt database");
				return;
			}

			var fis = m_root.List();
			SaveData();
			MoveOldFiles(fis);

			Console.WriteLine("Close database");
		}

		void MoveOldFiles(List<FileTreeItem> fis)
		{
			var logs = m_root[LogsName];
			if (logs == null)
				logs = m_root.CreateDir(LogsName);
			Debug.Assert(logs.IsDir);

			foreach (var fi in fis)
			{
				if (fi.IsDir)
					continue;
				switch (Path.GetExtension(fi.Name).ToLowerInvariant())
				{
					case ".log":
					case ".map":
						fi.MoveTo(logs, fi.Name);
						break;
				}
			}
		}
	}
}
