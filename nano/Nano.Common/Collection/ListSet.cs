using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Collection
{
	/// <summary>(Deprecated) ListSet</summary>
	/// <typeparam name="T">Value type</typeparam>
	/// <remarks>Deprecated class, do not use it.</remarks>
	public class ListSet<T>
	{
		List<T> m_list;
		Dictionary<T, int> m_dict;

		public ListSet()
		{
			m_list = new List<T>();
			m_dict = new Dictionary<T, int>();
		}

		public ListSet(int capacity)
		{
			m_list = new List<T>(capacity);
			m_dict = new Dictionary<T, int>(capacity);
		}

		public int Add(T key)
		{
			int idx;
			if (!m_dict.TryGetValue(key, out idx))
			{
				idx = m_list.Count;
				m_list.Add(key);
				m_dict.Add(key, idx);
			}
			return idx;
		}

		public T this[int index]
		{
			get { return m_list[index]; }
		}

		public int Count
		{
			get { return m_list.Count; }
		}
	}
}
