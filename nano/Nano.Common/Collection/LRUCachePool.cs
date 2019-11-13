using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Collection
{
	/// <summary>简单 LRU Cache 容器</summary>
	/// <typeparam name="TKey">键类型，通常为 string</typeparam>
	/// <typeparam name="TValue">值类型</typeparam>
	public class LRUCachePool<TKey, TValue> : IDisposable where TValue : class
	{
		class LRUCacheItem
		{
			public TKey Key;
			public TValue Value;
			public LRUCacheItem Prev, Next;
			public volatile int Lock;

			public LRUCacheItem(TKey _key, TValue _value, LRUCacheItem _prev, LRUCacheItem _next)
			{
				Key = _key;
				Value = _value;
				Prev = _prev;
				Next = _next;
				Lock = 0;
			}
		}

		/// <summary>销毁缓存对象的委托</summary>
		/// <param name="key">键</param>
		/// <param name="value">值对象</param>
		public delegate void OnObjectObsoletedDelegate(TKey key, TValue value);

		/// <summary>销毁缓存对象的委托实例</summary>
		public OnObjectObsoletedDelegate OnObjectObsoleted = (key, value) => { };

		/// <summary>创建新缓存对象的委托</summary>
		/// <param name="key">键</param>
		/// <returns>返回创建的值对象</returns>
		public delegate TValue CreateObjectDelegate(TKey key);

		/// <summary>创建新缓存对象的委托实例</summary>
		public CreateObjectDelegate CreateObject = null;

		int m_capacity, m_count;
		LRUCacheItem m_head, m_tail;
		Dictionary<TKey, LRUCacheItem> m_map;
		Dictionary<TKey, LRUCacheItem> m_obsoleted;

		/// <summary>构造新的 LRU Cache 容器</summary>
		/// <param name="capacity">缓存对象数目</param>
		public LRUCachePool(int capacity)
		{
			m_capacity = capacity;
			m_count = 0;
			m_head = m_tail = null;
			m_map = new Dictionary<TKey, LRUCacheItem>();
			m_obsoleted = new Dictionary<TKey, LRUCacheItem>();
		}

		/// <summary>获取缓存中所有对象的键</summary>
		/// <returns>返回保存键的数组</returns>
		public List<TKey> GetKeys()
		{
			var keys = new List<TKey>(m_count);
			for (LRUCacheItem item = m_head; item != null; item = item.Next)
				keys.Add(item.Key);
			return keys;
		}

		/// <summary>获取值对象</summary>
		/// <param name="key">键</param>
		/// <returns>返回对应的值对象，如果不存在，返回 null</returns>
		/// <remarks>获取的对象在使用完后，需要调用 Return 方法解锁。</remarks>
		public TValue Retrieve(TKey key)
		{
			lock (this)
			{
				LRUCacheItem item;
				if (!m_map.TryGetValue(key, out item))
					return null;

				if (m_head != item)
				{
					LRUCacheItem rprev = item.Prev, rnext = item.Next;

					if (item == m_tail)
						m_tail = item.Prev;

					item.Prev = null;
					item.Next = m_head;
					m_head.Prev = item;
					m_head = item;

					Debug.Assert(rprev != null);
					rprev.Next = rnext;
					if (rnext != null)
						rnext.Prev = rprev;
				}

				++item.Lock;
				return item.Value;
			}
		}

		/// <summary>解锁获取的值对象</summary>
		/// <param name="key">键</param>
		public void Return(TKey key)
		{
			lock (this)
			{
				LRUCacheItem item;
				if (m_map.TryGetValue(key, out item))
				{
					if (item.Lock <= 0)
						throw new ArgumentOutOfRangeException();
					--item.Lock;
					return;
				}

				item = m_obsoleted[key];
				if (item == null)
					throw new KeyNotFoundException();
				if (item.Lock <= 0)
					throw new ArgumentOutOfRangeException();
				if (--item.Lock <= 0)
				{
					OnObjectObsoleted(key, item.Value);
					m_obsoleted.Remove(key);
				}
			}
		}

		void _DoAdd(TKey key, TValue value)
		{
			if (m_map.ContainsKey(key))
				throw new ArgumentException("Key conflicted");

			if (m_count >= m_capacity)
			{
				// Delete LRU item
				LRUCacheItem rtail = m_tail;
				m_tail = rtail.Prev;
				m_tail.Next = null;
				--m_count;

				m_map.Remove(rtail.Key);
				if (rtail.Lock > 0)
					m_obsoleted.Add(rtail.Key, rtail);
				else
					OnObjectObsoleted(rtail.Key, rtail.Value);
			}

			LRUCacheItem item = new LRUCacheItem(key, value, null, m_head);
			if (m_head != null)
			{
				m_head.Prev = item;
				m_head = item;
			}
			else
			{
				m_head = m_tail = item;
			}
			m_map.Add(key, item);
			++m_count;
		}

		/// <summary>向容器添加一个给定的对象</summary>
		/// <param name="key">键</param>
		/// <param name="value">值对象</param>
		/// <remarks>
		/// 当容器中对象数目超过 capacity 给定的数目时，较久未使用（通过 Retrieve 方法簇）的对象将被清除。
		/// 如果该对象已经被解锁（锁计数为 0），它将被直接销毁，同时 OnObjectObsoleted 委托会被调用以便执行更多的手动资源释放。
		/// 如果该对象锁计数不为 0，它将被移动到 obsolete 容器中，待锁计数为 0 时执行销毁。
		/// </remarks>
		public void Add(TKey key, TValue value)
		{
			lock (this)
			{
				_DoAdd(key, value);
			}
		}

		/// <summary>根据给定的 Key 创建对象并添加到容器</summary>
		/// <param name="key">键</param>
		/// <remarks>该方法会调用 CreateObject 创建新的值对象，剩余的操作与 Add(key, value) 方法相同。</remarks>
		public void Add(TKey key)
		{
			lock (this)
			{
				TValue value = CreateObject(key);
				_DoAdd(key, value);
			}
		}

		/// <summary>获取值对象，如果不存在，则创建新对象</summary>
		/// <param name="key">键</param>
		/// <returns>返回对应的值对象，如果不存在，则调用 CreateObject 委托创建新对象。</returns>
		/// <remarks>获取的对象在使用完后，需要调用 Return 方法解锁。</remarks>
		public TValue RetrieveForce(TKey key)
		{
			lock (this)
			{
				if (!m_map.ContainsKey(key))
					Add(key);
				return Retrieve(key);
            }
		}

		/// <summary>内部数据检查</summary>
		/// <returns>如果没有发现错误，返回 true</returns>
		public bool CheckList()
		{
			lock (this)
			{
				// Count must be correct
				if (m_count > m_capacity || m_map.Count != m_count)
					return false;

				// Empty list
				if (m_count == 0)
					return m_head == null && m_tail == null;

				// Head & tail must be set for a list which is not empty
				if (m_head == null || m_tail == null)
					return false;

				LRUCacheItem prev = null, item = m_head;
				int count = 0;
				while (item != null)
				{
					++count;
					if (!m_map.ContainsKey(item.Key))
						return false;
					if (item.Prev != prev || item.Lock < 0)
						return false;

					prev = item;
					item = item.Next;
				}
				if (m_tail != prev || m_count != count)
					return false;

				foreach (var pair in m_obsoleted)
				{
					if (pair.Value.Lock <= 0)
						return false;
				}

				return true;
			}
		}

		/// <summary>强制从容器中移除给定对象</summary>
		/// <param name="key">键</param>
		/// <remarks>如果该对象被锁定，则抛出异常。</remarks>
		public void Retire(TKey key)
		{
			lock (this)
			{
				LRUCacheItem item;
				if (m_map.TryGetValue(key, out item))
				{
					if (item.Lock > 0)
						throw new Exception("LRU item locked");

					// delete item
					OnObjectObsoleted(key, item.Value);
					if (m_head != item)
						item.Prev.Next = item.Next;
					else
						m_head = item.Next;
					if (m_tail != item)
						item.Next.Prev = item.Prev;
					else
						m_tail = item.Prev;
					m_map.Remove(key);
					return;
				}
				if (m_obsoleted.TryGetValue(key, out item))
				{
					if (item.Lock > 0)
						throw new Exception("LRU item locked");

					throw new Exception("LRU item in wrong state");
				}
				// LRU item not found, nothing to do
			}
		}

		/// <summary>关闭整个容器</summary>
		public void Dispose()
		{
			lock (this)
			{
				foreach (var pair in m_map)
					OnObjectObsoleted(pair.Key, pair.Value.Value);
				foreach (var pair in m_obsoleted)
					OnObjectObsoleted(pair.Key, pair.Value.Value);
				m_map = m_obsoleted = null;
				m_count = m_capacity = 0;
				m_head = m_tail = null;
			}
		}
	}
}
