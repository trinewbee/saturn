using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Nano.Json;

namespace Nano.Ext.Persist
{
	public class BomList<T> : BomObject, IEnumerable<T>
	{
		List<T> m_list;

		public BomList(BomStore store) : base(store)
		{
			m_list = new List<T>();
		}

		public int Count
		{
			get { return m_list.Count; }
		}

		public T this[int index]
		{
			get { return m_list[index]; }
			set
			{
				m_list[index] = value;
				object v = MakeLogVal(value);
				object m = new Dictionary<string, object> { { "i", index }, { "v", v } };
				CustomCommand("ls:st", m);
			}
		}

		public void Add(T item)
		{
			m_list.Add(item);
			object v = MakeLogVal(item);
			CustomCommand("ls:ad", v);
		}

		public void Resize(int n, T item)
		{
			int count = m_list.Count;
			if (count < n)
			{
				for (int i = count; i < n; ++i)
					m_list.Add(item);

				object v = MakeLogVal(item);
				object m = new Dictionary<string, object> { { "n", n - count }, { "v", v } };
				CustomCommand("ls:ext", m);
			}
			else if (count > n)
			{
				m_list.RemoveRange(n, count - n);
				object m = new Dictionary<string, object> { { "i", n }, { "n", count - n } };
				CustomCommand("ls:red", m);
			}
		}

		static object MakeLogVal(T item)
		{
			BomObject o = item as BomObject;
			object v = o != null ? (object)o.m_bom_oi : (object)item;
			return v;
		}

		public override object SaverWriteCustomModel(BomSaver saver)
		{
			var model = new List<object>();
			if (BomTracker.IsBomType(typeof(T)))
			{
				foreach (T o in m_list)
				{
					int oi = saver.SaveObject(o as BomObject);
					model.Add(oi);
				}
			}
			else
			{
				Debug.Assert(BomTracker.IsValueType(typeof(T)));
				foreach (T o in m_list)
					model.Add(o);
			}
			return model;
		}

		public override void LoaderReadCustomModel(BomLoader kit, JsonNode jnModel)
		{
			Debug.Assert(jnModel.NodeType == JsonNodeType.NodeList);
			foreach (var jn in jnModel.ChildNodes)
			{
				object o = LoaderMakeValue(kit, typeof(T), jn);
				((System.Collections.IList)m_list).Add(o);
			}
		}

		public override void LoaderReadCustomBinlog(BomLoader kit, string cmd, JsonNode jnModel)
		{
			if (cmd == "ls:ad")
			{
				// {"c":"ls:ad","ti":2,"oi":2,"m":4}
				object o = LoaderMakeValue(kit, typeof(T), jnModel);
				((System.Collections.IList)m_list).Add(o);
			}
			else
				throw new NotSupportedException("Unknown custom bin-log command: " + cmd);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return m_list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_list.GetEnumerator();
		}
	}

	public class BomDictionary<KT, VT> : BomObject, IEnumerable<KeyValuePair<KT, VT>>
	{
		Dictionary<KT, VT> m_dict;

		public BomDictionary(BomStore store) : base(store)
		{
			m_dict = new Dictionary<KT, VT>();
		}

		public int Count
		{
			get { return m_dict.Count; }
		}

		public VT this[KT key]
		{
			get { return m_dict[key]; }
		}

		public bool ContainsKey(KT key)
		{
			return m_dict.ContainsKey(key);
		}

		/// <summary>读取指定键的值</summary>
		/// <param name="key">键</param>
		/// <param name="value">返回值</param>
		/// <returns>如果给定的键存在，返回 true</returns>
		/// <remarks>参见 Dictionary.TryGetValue 方法。</remarks>
		public bool TryGetValue(KT key, out VT value)
		{
			return m_dict.TryGetValue(key, out value);
		}

		public void Add(KT key, VT value)
		{
			m_dict.Add(key, value);
			BomObject o = key as BomObject;
			object ko = o != null ? (object)o.m_bom_oi : (object)key;
			o = value as BomObject;
			object vo = o != null ? (object)o.m_bom_oi : (object)value;
			object m = new Dictionary<string, object> { { "k", ko }, { "v", vo } };
			CustomCommand("dc:a", m);
		}

		public override object SaverWriteCustomModel(BomSaver saver)
		{
			var model = new List<object>();
			bool kbom = BomTracker.IsBomType(typeof(KT));
			bool vbom = BomTracker.IsBomType(typeof(VT));
			foreach (var pair in m_dict)
			{
				BomObject o = pair.Key as BomObject;
				object ko = o != null ? (object)saver.SaveObject(o) : (object)pair.Key;
				o = pair.Value as BomObject;
				object vo = o != null ? (object)saver.SaveObject(o) : (object)pair.Value;
				object po = new Dictionary<string, object> { { "k", ko }, { "v", vo } };
				model.Add(po);
			}
			return model;
		}

		public override void LoaderReadCustomModel(BomLoader kit, JsonNode jnModel)
		{
			Debug.Assert(jnModel.NodeType == JsonNodeType.NodeList);
			var dc = (System.Collections.IDictionary)m_dict;
			foreach (var jnPair in jnModel.ChildNodes)
			{
				var jnKey = jnPair["k"];
				var jnVal = jnPair["v"];
				object ko = LoaderMakeValue(kit, typeof(KT), jnKey);
				object vo = LoaderMakeValue(kit, typeof(VT), jnVal);
				dc.Add(ko, vo);
			}
		}

		public override void LoaderReadCustomBinlog(BomLoader kit, string cmd, JsonNode jnModel)
		{
			if (cmd == "dc:a")
			{
				// {"c":"dc:a","ti":3,"oi":3,"m":{"k":"Zhao","v":6}}
				var jnKey = jnModel["k"];
				var jnVal = jnModel["v"];
				object ko = LoaderMakeValue(kit, typeof(KT), jnKey);
				object vo = LoaderMakeValue(kit, typeof(VT), jnVal);
				((System.Collections.IDictionary)m_dict).Add(ko, vo);
			}
			else
				throw new NotSupportedException("Unknown custom bin-log command: " + cmd);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_dict.GetEnumerator();
		}

		IEnumerator<KeyValuePair<KT, VT>> IEnumerable<KeyValuePair<KT, VT>>.GetEnumerator()
		{
			return m_dict.GetEnumerator();
		}
	}
}
