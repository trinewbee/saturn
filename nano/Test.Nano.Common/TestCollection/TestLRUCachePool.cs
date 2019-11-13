using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Collection;
using Nano.UnitTest;

namespace TestCommon.TestCollection
{
	class TestLRUCachePool
	{
		static void testAddItems()
		{
			List<string> obsoleted = new List<string>();
			LRUCachePool<string, object> pool = new LRUCachePool<string, object>(3);
			pool.OnObjectObsoleted = (key, value) => obsoleted.Add(key);

			_CheckKeys(pool);
			Test.Assert(obsoleted.Count == 0);

			pool.Add("a", 1);
			_CheckKeys(pool, "a");
			Test.Assert(obsoleted.Count == 0);

			pool.Add("b", 2);
			_CheckKeys(pool, "b", "a");
			Test.Assert(obsoleted.Count == 0);

			pool.Add("c", 3);
			pool.Add("d", 4);
			_CheckKeys(pool, "d", "c", "b");
			Test.Assert(obsoleted.Count == 1);

			pool.Add("e", 5);
			pool.Add("f", 6);
			pool.Add("g", 7);
			_CheckKeys(pool, "g", "f", "e");
			Test.Assert(obsoleted.Count == 4);

			pool.Dispose();
			Test.Assert(obsoleted.Count == 7);
		}

		static void testUseItems()
		{
			List<string> obsoleted = new List<string>();
			LRUCachePool<string, object> pool = new LRUCachePool<string, object>(3);
			pool.OnObjectObsoleted = (key, value) => obsoleted.Add(key);

			pool.Add("a", 1);
			Test.Assert((int)pool.Retrieve("a") == 1);
			pool.Return("a");
			Test.Assert(pool.CheckList());

			pool.Add("b", 2);
			pool.Add("c", 3);
			_CheckKeys(pool, "c", "b", "a");

			Test.Assert((int)pool.Retrieve("a") == 1);
			pool.Return("a");
			_CheckKeys(pool, "a", "c", "b");

			Test.Assert((int)pool.Retrieve("b") == 2);
			pool.Return("b");
			_CheckKeys(pool, "b", "a", "c");

			Test.Assert((int)pool.Retrieve("a") == 1);
			pool.Return("a");
			_CheckKeys(pool, "a", "b", "c");

			pool.Add("d", 4);
			_CheckKeys(pool, "d", "a", "b");
			Test.Assert(obsoleted.Count == 1);

			pool.Dispose();
			Test.Assert(obsoleted.Count == 4);
		}

		static void testObsoletedItems()
		{
			List<string> obsoleted = new List<string>();
			LRUCachePool<string, object> pool = new LRUCachePool<string, object>(3);
			pool.OnObjectObsoleted = (key, value) => obsoleted.Add(key);

			pool.Add("a", 1);
			pool.Add("b", 2);
			pool.Add("c", 3);

			pool.Retrieve("a");
			pool.Retrieve("b");
			pool.Return("b");
			pool.Retrieve("c");
			pool.Return("c");
			_CheckKeys(pool, "c", "b", "a");

			pool.Add("d", 4);
			_CheckKeys(pool, "d", "c", "b");
			Test.Assert(obsoleted.Count == 0);

			pool.Return("a");
			Test.Assert(obsoleted.Count == 1);

			pool.Dispose();
			Test.Assert(obsoleted.Count == 4);
		}

		static void testRetrieveForce()
		{
			LRUCachePool<string, object> pool = new LRUCachePool<string, object>(3);
			pool.CreateObject = key => key;

			pool.Add("a", "aaa");
			pool.Add("b");
			Test.Assert((string)pool.Retrieve("a") == "aaa");
			pool.Return("a");
			Test.Assert((string)pool.Retrieve("b") == "b");
			pool.Return("b");

			Test.Assert((string)pool.RetrieveForce("a") == "aaa");
            pool.Return("a");
			Test.Assert((string)pool.RetrieveForce("c") == "c");
			pool.Return("c");
		}

		#region Kit methods

		static void _CheckKeys(LRUCachePool<string, object> pool, params string[] keys)
		{
			Test.Assert(pool.CheckList());
			List<string> keys_t = pool.GetKeys();
			Test.AssertListEqual(keys, keys_t);
        }

		#endregion

		public static void Run()
		{
			Console.WriteLine("TestCollection.TestLRUCachePool");
			testAddItems();
			testUseItems();
			testObsoletedItems();
			testRetrieveForce();
        }
	}
}
