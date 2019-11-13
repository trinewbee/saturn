using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Collection
{
	/// <summary>环形缓冲区</summary>
	/// <typeparam name="T">保存的对象类型</typeparam>
	public class RingBuffer<T> : IEnumerable<T>, IEnumerable
	{
		public int Capacity { get; private set; }
		public int Count { get; private set; }

		T[] m_items;
		int m_tail;

		/// <summary>初始化一个环形缓冲区集合</summary>
		/// <param name="capacity">缓冲区最大容纳元素个数</param>
		public RingBuffer(int capacity)
		{
			Capacity = capacity;
			Count = 0;

			m_items = new T[capacity];
			m_tail = 0;
		}

		/// <summary>返回集合中给定位置的项目</summary>
		/// <param name="index">项目位置，其值应大于等于 0，小于 Count</param>
		/// <returns>返回对应位置项目</returns>
		public T this[int index]
		{
			get
			{
				lock (this)
					return m_items[(m_tail + index - Count + Capacity) % Capacity];
			}
		}

		/// <summary>获取最近的项目</summary>
		/// <param name="count">获取的项目数量</param>
		/// <returns>返回最近的给定数量的项目，如果给定数量大于集合中的所有项目数，则返回所有项目</returns>
		public T[] GetLastItems(int count)
		{
			if (count < 0)
				throw new ArgumentException();

			lock (this)
			{
				int start = _GetLastRange(ref count);
				return _GetResult(start, count);
			}
		}

		int  _GetLastRange(ref int count)
		{
			count = Math.Min(count, Count);
			var start = (m_tail + Capacity - count) % Capacity;
			return start;
		}

		T[] _GetResult(int start, int count)
		{
			var items = new T[count];
			for (int i = 0; i < count; ++i)
				items[i] = m_items[(start + i) % Capacity];
			return items;
		}

		/// <summary>获取最近的符合给定条件的连续项目</summary>
		/// <param name="filter">条件函数</param>
		/// <returns>返回一个数组，其每个元素都符合给定条件</returns>
		/// <remarks>
		/// 本函数会按照加入顺序逆序寻找第一个不符合给定条件的项目，并返回该项目之后的所有项目。
		/// 如果所有项目都符合给定条件，则返回缓冲区的所有项目。
		/// </remarks>
		public T[] GetUpdatedItems(Predicate<T> filter)
		{
			lock (this)
			{
				var r = _GetRecentStart(filter);
				return _GetResult(r.Item1, r.Item2);
			}
		}

		// (int start, int count)
		Tuple<int, int> _GetRecentStart(Predicate<T> filter)
		{
			int st = m_tail - Count; // [-Capacity, Capacity)
			int st1 = st > 0 ? st : 0;
			for (var i = m_tail; i > st1; )
			{
				if (!filter(m_items[--i]))
					return new Tuple<int, int>(i + 1, m_tail - (i + 1));
			}
			if (st < 0)
			{
				st1 = st + Capacity;
				for (var i = Capacity; i > st1; )
				{
					if (!filter(m_items[--i]))
						return new Tuple<int, int>(i + 1, Capacity - (i + 1) + m_tail);
				}
				return new Tuple<int, int>(st1, Capacity - st1 + m_tail);
			}
			else
				return new Tuple<int, int>(st, m_tail - st);
		}

		/// <summary>追加一个元素</summary>
		/// <param name="item">待添加元素</param>
		public void Add(T item)
		{
			lock (this)
			{
				m_items[m_tail] = item;
				m_tail = (m_tail + 1) % Capacity;
				if (Count < Capacity)
					++Count;
			}
		}

		/// <summary>清空整个集合</summary>
		public void Clear()
		{
			lock (this)
				Count = m_tail = 0;
		}

		/// <summary>自检函数</summary>
		/// <returns>返回自检是否成功</returns>
		public bool _Check()
		{
			if (Count < 0 || Count > Capacity || m_tail < 0 || m_tail >= Capacity)
				return false;
			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			var st = (m_tail + Capacity - Count) % Capacity;
			for (int i = 0; i < Count; ++i)
				yield return m_items[(st + i) % Capacity];
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
