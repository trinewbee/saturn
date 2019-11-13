using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Collection
{
	public abstract class ListTableItem<K>
	{
		protected int m_index;
		protected K m_key;

		protected ListTableItem(K _key)
		{
			m_index = 0;
			m_key = _key;
		}

		protected ListTableItem(int _index, K _key)
		{
			m_index = _index;
			m_key = _key;
		}

		public int Index
		{
			get { return m_index; }
		}

		public K Key
		{
			get { return m_key; }
		}

		public void _SetIndex(int index)
		{
			Debug.Assert(m_index == 0 && index != 0);
			m_index = index;
		}
	}

	/// <summary>A table of items with a key and an index</summary>
	/// <typeparam name="K">Type of key</typeparam>
	/// <typeparam name="T">Type of value</typeparam>
	/// <remarks>
	/// The index may be assigned by the collection.
	/// Note that the index should not be 0.
	/// </remarks>
	public class ListTable<K, T> : IEnumerable<T> where T : ListTableItem<K>
	{
		List<T> m_list;
		Dictionary<K, T> m_dict;
		int m_count;

		public ListTable()
		{
			m_list = new List<T>();
			m_list.Add(null);	// The index can't be 0
			m_dict = new Dictionary<K, T>();
			m_count = 0;
		}

		public int Count
		{
			get { return m_count; }
		}

		public T this[int index]
		{
			get { return index < m_list.Count ? m_list[index] : null; }
		}

		public T this[K key]
		{
			get
			{
				T item;
				if (m_dict.TryGetValue(key, out item))
					return item;
				else
					return null;
			}
		}

		public bool HasKey(K key)
		{
			return m_dict.ContainsKey(key);
		}

		public void Clear()
		{
			m_list.Clear();
			m_list.Add(null);
			m_dict.Clear();
			m_count = 0;
		}

		/// <summary>Add an item</summary>
		/// <param name="item">The item to be added</param>
		/// <remarks>
		/// If index of item is 0, a new index is assigned by the collection.
		/// If index is not 0, and an item exists in the place of index, an
		/// IndexOutOfRangeException will be thrown.
		/// If key is null, an ArgumentNullException is thrown.
		/// If key exists in the collection, an ArgumentException is thrown.
		/// </remarks>
		public void Add(T item)
		{
			if (item.Index == 0)
				item._SetIndex(m_list.Count);

			m_dict.Add(item.Key, item);

			for (int i = m_list.Count; i <= item.Index; ++i)
				m_list.Add(null);

			if (m_list[item.Index] != null)
			{
				m_dict.Remove(item.Key);
				throw new IndexOutOfRangeException("The index specified already contains value");
			}

			m_list[item.Index] = item;
			++m_count;
		}

		public bool RemoveAt(int index)
		{
			if (index >= m_list.Count || m_list[index] == null)
				return false;

			T item = m_list[index];
			m_list[index] = null;
			m_dict.Remove(item.Key);
			--m_count;
			return true;
		}

		public bool Remove(K key)
		{
			T item;
			if (m_dict.TryGetValue(key, out item))
			{
				m_dict.Remove(key);
				Debug.Assert(m_list[item.Index] == item);
				m_list[item.Index] = null;
				--m_count;
				return true;
			}
			else
				return false;
		}

		#region Enumerator

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			foreach (T item in m_list)
			{
				if (item != null)
					yield return item;
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			foreach (T item in m_list)
			{
				if (item != null)
					yield return item;
			}
		}

		#endregion
	}
}
