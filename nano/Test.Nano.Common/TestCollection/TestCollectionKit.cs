using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Common;
using Nano.Collection;
using Nano.UnitTest;

namespace TestCommon.TestCollection
{
	class TestCollectionKit
	{
		class TB {
			public int X;
		}

		class TD : TB
		{
			public int Y;
		}

		static void testWalk()
		{
			string value = "";
			Action<int> tr = (int x) => value += x.ToString() + ',';

			int[] s = new int[] { 3, 6, 5 };
			CollectionKit.WalkGeneral(s, tr);
			Test.Assert(value == "3,6,5,");

			value = "";
			CollectionKit.WalkGeneral(new int[0], tr);
			Test.Assert(value == "");
		}

		static void testCopy()
		{
			int[] s = new int[] { 3, 6, 5 };
			List<int> t = new List<int>();
			CollectionKit.CopyGeneral(s, t);
			Test.Assert(t.Count == 3 && t[0] == 3 && t[1] == 6 && t[2] == 5);

			CollectionKit.CopyGeneral(new int[] { 8 }, t);
			Test.Assert(t.Count == 4 && t[0] == 3 && t[1] == 6 && t[2] == 5 && t[3] == 8);

			t = new List<int>();
			CollectionKit.CopyGeneral(new int[0], t);
			Test.Assert(t.Count == 0);
		}

		static void testToList()
		{
			int[] s = new int[] { 9, 2, 8 };
			List<int> t = CollectionKit.ToList(s);
			Test.Assert(t.Count == 3 && t[0] == 9 && t[2] == 8);

			TD[] sd = new TD[] { new TD { X = 1, Y = 1 }, new TD { X = 2, Y = 4 }, new TD { X = 3, Y = 9 } };
			List<TD> lsd = CollectionKit.ToList<TD>(sd);
			Test.Assert(lsd.Count == 3 && lsd[0].Y == 1 && lsd[2].Y == 9);

			List<TB> lsb = CollectionKit.ToList<TB>(sd);
			Test.Assert(lsb.Count == 3 && lsb[0].X == 1 && lsb[2].X == 3);

			lsd = CollectionKit.ToListCast<TB, TD>(lsb);
			Test.Assert(lsd.Count == 3 && lsd[0].Y == 1 && lsd[2].Y == 9);
		}

		static void testToArray()
		{
			List<int> s = new List<int>(new int[] { 3, 6, 5 });
			int[] t = CollectionKit.ToArray(s);
			Test.Assert(t.Length == 3 && t[0] == 3 && t[1] == 6 && t[2] == 5);

			t = CollectionKit.ToArray(s, 4);
			Test.Assert(t.Length == 4 && t[0] == 3 && t[1] == 6 && t[2] == 5 && t[3] == 0);

			t = CollectionKit.ToArray(s, 3);
			Test.Assert(t.Length == 3 && t[0] == 3 && t[1] == 6 && t[2] == 5);

			t = CollectionKit.ToArray(s, 2);
			Test.Assert(t.Length == 2 && t[0] == 3 && t[1] == 6);

			Test.Assert(CollectionKit.ToArray(s, 0).Length == 0);

			Test.Assert(CollectionKit.ToArray(new List<int>()).Length == 0);
		}

		static void testTransform()
		{
			Func<string, int> t = (string s) => Convert.ToInt32(s);

			string[] st = new string[] { "98", "76", "54" };
			List<int> tt = CollectionKit.Transform<string, int>(st, t);
			Test.Assert(tt.Count == 3 && tt[0] == 98 && tt[1] == 76 && tt[2] == 54);

			int[] ta = CollectionKit.Transform<string, int>(st, t, 4);
			Test.Assert(ta.Length == 4 && ta[0] == 98 && ta[1] == 76 && ta[2] == 54 && ta[3] == 0);

			ta = CollectionKit.Transform<string, int>(st, t, 3);
			Test.Assert(ta.Length == 3 && ta[0] == 98 && ta[1] == 76 && ta[2] == 54);

			ta = CollectionKit.Transform<string, int>(st, t, 2);
			Test.Assert(ta.Length == 2 && ta[0] == 98 && ta[1] == 76);

			Test.Assert(CollectionKit.Transform<string, int>(st, t, 0).Length == 0);

			Test.Assert(CollectionKit.Transform<string, int>(new string[0], t).Count == 0);
		}

		static void testFind()
		{
			int[] arr = new int[] { 1, 9, 8, 1, 9, 3 };

			Predicate<int> cond1 = x => x == 1;
			Predicate<int> cond9 = x => x == 9;
			Predicate<int> cond2 = x => x == 2;

			Test.Assert(CollectionKit.FindIndex(arr, 0, cond1) == 0);
			Test.Assert(CollectionKit.FindIndex(arr, 0, cond9) == 1);
			Test.Assert(CollectionKit.FindIndex(arr, 0, cond2) == -1);
			Test.Assert(CollectionKit.FindIndex(arr, 0, cond1) == 0);
			Test.Assert(CollectionKit.FindIndex(arr, 1, cond1) == 3);
			Test.Assert(CollectionKit.FindIndex(arr, 4, cond1) == -1);

			Test.Assert(CollectionKit.FindIndex(arr, 0, 1) == 0);
			Test.Assert(CollectionKit.FindIndex(arr, 0, 9) == 1);
			Test.Assert(CollectionKit.FindIndex(arr, 0, 2) == -1);
			Test.Assert(CollectionKit.FindIndex(arr, 0, 1) == 0);
			Test.Assert(CollectionKit.FindIndex(arr, 1, 1) == 3);
			Test.Assert(CollectionKit.FindIndex(arr, 4, 1) == -1);

			Test.Assert(CollectionKit.Find(arr, 1, cond1) == 1);
			Test.Assert(CollectionKit.Find(arr, 4, cond1) == 0);

			Test.Assert(CollectionKit.Find(arr, cond1) == 1);
			Test.Assert(CollectionKit.Find(arr, cond9) == 9);
			Test.Assert(CollectionKit.Find(arr, cond2) == 0);

			Test.Assert(CollectionKit.FindLastIndex(arr, -1, cond1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, -1, cond9) == 4);
			Test.Assert(CollectionKit.FindLastIndex(arr, -1, cond2) == -1);
			Test.Assert(CollectionKit.FindLastIndex(arr, arr.Length - 1, cond1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, 3, cond1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, 2, cond1) == 0);
			Test.Assert(CollectionKit.FindLastIndex(arr, 0, cond9) == -1);

			Test.Assert(CollectionKit.FindLastIndex(arr, -1, 1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, -1, 9) == 4);
			Test.Assert(CollectionKit.FindLastIndex(arr, -1, 2) == -1);
			Test.Assert(CollectionKit.FindLastIndex(arr, arr.Length - 1, 1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, 3, 1) == 3);
			Test.Assert(CollectionKit.FindLastIndex(arr, 2, 1) == 0);
			Test.Assert(CollectionKit.FindLastIndex(arr, 0, 9) == -1);

			Test.Assert(CollectionKit.FindLast(arr, -1, cond9) == 9);
			Test.Assert(CollectionKit.FindLast(arr, 1, cond9) == 9);
			Test.Assert(CollectionKit.FindLast(arr, 0, cond9) == 0);
			Test.Assert(CollectionKit.Find(arr, 4, cond1) == 0);
		}

		static void testSelect()
		{
			int[] arr = new int[] { 1, 9, 8, 1, 9, 3 };
			var e = CollectionKit.SelectWalk(arr, x => x >= 3);
			int[] r = CollectionKit.ToList(e).ToArray();
			int[] t = new int[] { 9, 8, 9, 3 };
			Test.Assert(CmpKit.ListEq(r, t, CmpKit.Eq));

			r = CollectionKit.Select(arr, x => x <= 3).ToArray();
			t = new int[] { 1, 1, 3 };
			Test.Assert(CmpKit.ListEq(r, t, CmpKit.Eq));
		}

		static void testCount()
		{
			int[] arr = new int[] { 1, 9, 8, 1, 9, 3 };
			Test.Assert(CollectionKit.Count(arr, x => x >= 3) == 4);
			Test.Assert(CollectionKit.Count(arr, x => x < 1) == 0);
		}

		public static void Run()
		{
			Console.WriteLine("TestCollection.TestCollectionKit");
			testWalk();
			testCopy();
			testToList();
			testToArray();
			testTransform();
			testFind();
			testSelect();
			testCount();
        }
	}
}
