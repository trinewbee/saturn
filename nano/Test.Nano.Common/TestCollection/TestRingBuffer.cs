using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nano.Collection;
using Nano.UnitTest;

namespace TestCommon.TestCollection
{
	class RingBufferSpy<T>
	{
		static Type m_vt;
		static FieldInfo m_fi_tail, m_fi_items;

		static RingBufferSpy()
		{
			var binding = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			m_vt = typeof(RingBuffer<T>);
			m_fi_tail = m_vt.GetField("m_tail", binding);
			m_fi_items = m_vt.GetField("m_items", binding);
		}

		RingBuffer<T> m_obj;

		public RingBufferSpy(RingBuffer<T> obj)
		{
			m_obj = obj;
		}

		public int Tail => (int)m_fi_tail.GetValue(m_obj);

		public T[] Items => (T[])m_fi_items.GetValue(m_obj);
	}

	class TestRingBuffer
	{
		RingBuffer<int> m_rb;
		RingBufferSpy<int> m_spy;

		TestRingBuffer()
		{
			m_rb = new RingBuffer<int>(5);
			_check();
			Test.Assert(m_rb.Count == 0 && m_rb.Capacity == 5);

			m_spy = new RingBufferSpy<int>(m_rb);
			Test.Assert(m_spy.Tail == 0 && m_spy.Items.Length == 5);
		}

		void testAdd()
		{
			for (int i = 1; i < 5; ++i)
			{
				m_rb.Add(i);
				_check();
				Test.Assert(m_rb.Count == i && m_rb.Capacity == 5 && m_spy.Tail == i);
			}
			for (int i = 5; i < 10; ++i)
			{
				m_rb.Add(i);
				_check();
				Test.Assert(m_rb.Count == 5 && m_rb.Capacity == 5 && m_spy.Tail == i - 5);
			}
			m_rb.Add(10);
			_check();
			Test.Assert(m_rb.Count == 5 && m_rb.Capacity == 5 && m_spy.Tail == 0);
			_clear();
		}

		void testGetItem()
		{
			for (int i = 1; i <= 3; ++i)
				m_rb.Add(i);
			Test.Assert(m_rb.Count == 3 && m_spy.Tail == 3);
			_check();
			Test.Assert(m_rb[0] == 1 && m_rb[2] == 3);
			_clear();

			for (int i = 1; i <= 8; ++i)
				m_rb.Add(i);
			_check();
			Test.Assert(m_rb.Count == 5 && m_spy.Tail == 3);
			Test.AssertListEqual(m_spy.Items, new int[] { 6, 7, 8, 4, 5 });
			Test.Assert(m_rb[0] == 4 && m_rb[1] == 5 && m_rb[2] == 6 && m_rb[4] == 8);
			_clear();

			for (int i = 1; i <= 10; ++i)
				m_rb.Add(i);
			_check();
			Test.Assert(m_rb.Count == 5 && m_spy.Tail == 0);
			Test.AssertListEqual(m_spy.Items, new int[] { 6, 7, 8, 9, 10 });
			Test.Assert(m_rb[0] == 6 && m_rb[4] == 10);
			_clear();
		}

		void testEnum()
		{
			for (int i = 1; i <= 3; ++i)
				m_rb.Add(i);
			var items = CollectionKit.ToList(m_rb);
			Test.AssertListEqual(items, new int[] { 1, 2, 3 });
			_clear();

			for (int i = 1; i <= 8; ++i)
				m_rb.Add(i);
			items = CollectionKit.ToList(m_rb);
			Test.AssertListEqual(items, new int[] { 4, 5, 6, 7, 8 });
			_clear();

			for (int i = 1; i <= 10; ++i)
				m_rb.Add(i);
			items = CollectionKit.ToList(m_rb);
			Test.AssertListEqual(items, new int[] { 6, 7, 8, 9, 10 });
			_clear();
		}

		void testGetLastItems_1()
		{
			// 结果没有回绕
			for (int i = 1; i <= 3; ++i)
				m_rb.Add(i);

			var items = m_rb.GetLastItems(0);
			Test.Assert(items.Length == 0);

			items = m_rb.GetLastItems(2);
			Test.AssertListEqual(items, new int[] { 2, 3 });

			items = m_rb.GetLastItems(3);
			Test.AssertListEqual(items, new int[] { 1, 2, 3 });

			items = m_rb.GetLastItems(4);
			Test.AssertListEqual(items, new int[] { 1, 2, 3 });

			_clear();
		}

		void testGetLastItems_2()
		{
			// 结果回绕
			for (int i = 1; i <= 8; ++i)
				m_rb.Add(i);

			// 6, 7, 8, 4, 5
			var items = m_rb.GetLastItems(2);
			Test.AssertListEqual(items, new int[] { 7, 8 });

			items = m_rb.GetLastItems(3);
			Test.AssertListEqual(items, new int[] { 6, 7, 8 });

			items = m_rb.GetLastItems(5);
			Test.AssertListEqual(items, new int[] { 4, 5, 6, 7, 8 });

			items = m_rb.GetLastItems(6);
			Test.AssertListEqual(items, new int[] { 4, 5, 6, 7, 8 });

			_clear();
		}

		void testGetLastItems_3()
		{
			// 临界点 (m_tail = 0) 结果回绕
			for (int i = 1; i <= 10; ++i)
				m_rb.Add(i);

			// 6, 7, 8, 9, 10
			var items = m_rb.GetLastItems(0);
			Test.Assert(items.Length == 0);

			items = m_rb.GetLastItems(1);
			Test.AssertListEqual(items, new int[] { 10 });

			items = m_rb.GetLastItems(5);
			Test.AssertListEqual(items, new int[] { 6, 7, 8, 9, 10 });

			items = m_rb.GetLastItems(6);
			Test.AssertListEqual(items, new int[] { 6, 7, 8, 9, 10 });

			_clear();
		}

		void testGetUpdatedItems_1()
		{
			// 结果没有回绕
			for (int i = 1; i <= 3; ++i)
				m_rb.Add(i);

			// 1, 2, 3
			var items = m_rb.GetUpdatedItems(x => x > 3);
			Test.Assert(items.Length == 0);

			items = m_rb.GetUpdatedItems(x => x >= 2);
			Test.AssertListEqual(items, new int[] { 2, 3 });

			items = m_rb.GetUpdatedItems(x => x >= 1);
			Test.AssertListEqual(items, new int[] { 1, 2, 3 });

			_clear();
		}

		void testGetUpdatedItems_2()
		{
			// 结果回绕
			for (int i = 1; i <= 8; ++i)
				m_rb.Add(i);

			// 6, 7, 8, 4, 5
			var items = m_rb.GetUpdatedItems(x => x > 8);
			Test.Assert(items.Length == 0);
				
			items = m_rb.GetUpdatedItems(x => x >= 7);
			Test.AssertListEqual(items, new int[] { 7, 8 });

			items = m_rb.GetUpdatedItems(x => x >= 6);
			Test.AssertListEqual(items, new int[] { 6, 7, 8 });

			items = m_rb.GetUpdatedItems(x => x >= 5);
			Test.AssertListEqual(items, new int[] { 5, 6, 7, 8 });

			items = m_rb.GetUpdatedItems(x => x >= 4);
			Test.AssertListEqual(items, new int[] { 4, 5, 6, 7, 8 });

			_clear();
		}

		void testGetUpdatedItems_3()
		{
			// 临界点 (m_tail = 0) 结果回绕
			for (int i = 1; i <= 10; ++i)
				m_rb.Add(i);

			// 6, 7, 8, 9, 10
			var items = m_rb.GetUpdatedItems(x => x > 10);
			Test.Assert(items.Length == 0);

			items = m_rb.GetUpdatedItems(x => x >= 10);
			Test.AssertListEqual(items, new int[] { 10 });

			items = m_rb.GetUpdatedItems(x => x >= 7);
			Test.AssertListEqual(items, new int[] { 7, 8, 9, 10 });

			items = m_rb.GetUpdatedItems(x => x >= 6);
			Test.AssertListEqual(items, new int[] { 6, 7, 8, 9, 10 });

			_clear();
		}

		void _check() => Test.Assert(m_rb._Check());

		void _clear()
		{
			m_rb.Clear();
			_check();
			Test.Assert(m_rb.Count == 0 && m_rb.Capacity == 5 && m_spy.Tail == 0);
		}

		public static void Run()
		{
			Console.WriteLine("TestCollection.TestRingBuffer");
			var o = new TestRingBuffer();
			o.testAdd();
			o.testGetItem();
			o.testEnum();
			o.testGetLastItems_1();
			o.testGetLastItems_2();
			o.testGetLastItems_3();
			o.testGetUpdatedItems_1();
			o.testGetUpdatedItems_2();
			o.testGetUpdatedItems_3();
		}
	}
}
