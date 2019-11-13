using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nano.Common;

namespace Nano.Storage.Backward
{
	// deprecated
	public interface IKeyLocator
	{
		void Open(string path, bool fCreate);

		string Get(BinaryValue key);

		IEnumerable<string> List();
	}

	public class TailKeyLocator : IKeyLocator
	{
		string m_path = null;

		public void Open(string path, bool fCreate)
		{
			m_path = path;
			if (fCreate)
				Directory.CreateDirectory(path);
		}

		public string Get(BinaryValue key)
		{
			string key_s = key.ToString();
			return Path.Combine(m_path, key_s);
		}

		public IEnumerable<string> List()
		{
			string[] ss = Directory.GetFiles(m_path);
			foreach (string s in ss)
				yield return Path.GetFileName(s);
		}
	}

	public class LocalKeyValueStorage : IKeyValueStorage
	{
		IKeyLocator m_loc;

		public LocalKeyValueStorage(IKeyLocator loc)
		{
			m_loc = loc;
		}

		public void Open(string path, bool fCreate)
		{
			m_loc.Open(path, fCreate);
		}

		public RewriteCapability RewriteCaps
		{
			get { return RewriteCapability.WritableBehavior; }
		}

		public IEnumerable<BinaryValue> ListKeys()
		{
			foreach (var name in m_loc.List())
				yield return BinaryValue.FromString(name);
		}

		public bool HasKey(BinaryValue key)
		{
			string path = m_loc.Get(key);
			return File.Exists(path);
		}

		public System.IO.Stream CreateFile(BinaryValue key, long size)
		{
			string path = m_loc.Get(key);
			return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
		}

		public System.IO.Stream OpenFile(BinaryValue key, DataOpenMode mode)
		{
			FileAccess access = mode != DataOpenMode.Read ? FileAccess.ReadWrite : FileAccess.Read;
			string path = m_loc.Get(key);
			return new FileStream(path, FileMode.Open, access);			
		}

		public IEnumerable<string> List()
		{
			return m_loc.List();
		}
	}
}
