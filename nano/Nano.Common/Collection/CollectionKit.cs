using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Collection
{
	/// <summary>简单的集合操作辅助类</summary>
	public static class CollectionKit
	{
		#region Comparisons

		/// <summary>用于 int 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int IntComparison(int x, int y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 uint 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int UIntComparison(uint x, uint y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 long 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int Int64Comparison(long x, long y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 ulong 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int UInt64Comparison(ulong x, ulong y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 float 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int SingleComparison(float x, float y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 double 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int DoubleComparison(double x, double y) => x == y ? 0 : (x > y ? 1 : -1);

		/// <summary>用于 byte 类型的比较函数</summary>
		/// <param name="x">x</param>
		/// <param name="y">y</param>
		/// <returns>如果两数相当返回 0，如果 x > y 返回正数，如果 x &lgt; y 返回负数</returns>
		public static int ByteComparison(byte x, byte y) => x == y ? 0 : (x > y ? 1 : -1);

		#endregion

		/// <summary>遍历一个集合（枚举器）</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="s">枚举器</param>
		/// <param name="t">操作委托</param>
		/// <remarks>
		/// 本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托
		/// </remarks>
		public static void WalkGeneral<T>(IEnumerable<T> s, Action<T> t)
		{
			foreach (T item in s)
				t(item);
		}

		/// <summary>遍历枚举器，将枚举的对象放到给定集合</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="s">源对象集合的枚举器</param>
		/// <param name="t">要复制到的目标对象集合</param>
		public static void CopyGeneral<T>(IEnumerable<T> s, ICollection<T> t)
		{
			foreach (T item in s)
				t.Add(item);
		}

        /// <summary>遍历枚举器，将枚举的对象转化为list集合</summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="e">源对象集合的枚举器</param>
        /// <returns>返回的转化后的list集合</returns>
		public static List<T> ToList<T>(IEnumerable<T> e)
		{
			List<T> rs = new List<T>();
			CopyGeneral<T>(e, rs);
			return rs;
		}

        /// <summary>
        /// 遍历一个枚举器，将指的对象（S）转换成另外一种指定对象（T）（S-->T），但是转化后的对象（T）需要为需要转化对象（S）类的子类
        /// </summary>
        /// <typeparam name="S">需要转换的对象类型</typeparam>
        /// <typeparam name="T">转换后的对象类型</typeparam>
        /// <param name="e">枚举器</param>
        /// <returns>返回转化后对象类型（T）的list集合</returns>
        public static List<T> ToListCast<S, T>(IEnumerable<S> e) where T : S
		{
			return Transform<S, T>(e, x => (T)x);
		}

		/// <summary>根据给定枚举器构建数组</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="e">枚举器</param>
		/// <param name="count">数组长度</param>
		/// <returns>返回构造的数组</returns>
		/// <remarks>
		/// 如果实际枚举的对象数目小于给定的 count，则数组空余的位置为该类型的缺省值。
		/// 如果实际枚举的对象数目大于给定的 count，则多余的对象被丢弃。
		/// </remarks>
		public static T[] ToArray<T>(IEnumerable<T> e, int count)
		{
			T[] tarray = new T[count];
			int i = 0;
			foreach (T item in e)
			{
				if (i >= count)
					break;
				tarray[i++] = item;
			}
			return tarray;
		}

        /// <summary>
        /// 遍历一个list集合，将list集合转化为数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="list">需要遍历的list集合</param>
        /// <returns>返回转化后的数组（数组长度为list集合的大小）</returns>
		public static T[] ToArray<T>(IList<T> list)
		{
			return ToArray<T>(list, list.Count);
		}

        /// <summary>
        /// 遍历一个list集合，将list集合中指定其实位置与长度的元素转化为数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="list">需要遍历的list集合</param>
        /// <param name="off">起始位置</param>
        /// <param name="len">数组对象的数量</param>
        /// <returns>返回转化后的数组</returns>
		public static T[] ToArray<T>(IList<T> list, int off, int len)
		{
			T[] r = new T[len];
			for (int i = 0; i < len; ++i)
				r[i] = list[i + off];
			return r;
		}

		/// <summary>根据给定的数组的一段对象序列构造新数组</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="arr">原数组</param>
		/// <param name="off">起始位置</param>
		/// <param name="len">对象数量</param>
		/// <returns>返回构造的新数组</returns>
		public static T[] ToArray<T>(T[] arr, int off, int len)
		{
			T[] r = new T[len];
			Array.Copy(arr, off, r, 0, len);
			return r;
		}

        /// <summary>
        /// 遍历一个枚举器，将指的对象（S）转换成另外一种指定对象（T）（S-->T）
        /// </summary>
        /// <typeparam name="S">需要转换的对象类型</typeparam>
        /// <typeparam name="T">转换后的对象类型</typeparam>
        /// <param name="s">枚举器</param>
        /// <param name="t">操作委托</param>
        /// <returns>返回转化后对象类型（T）的list集合</returns>
        /// <remarks>
        /// 本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托
        /// </remarks>
		public static List<T> Transform<S, T>(IEnumerable<S> s, Func<S, T> t)
		{
			List<T> tlist = new List<T>();
			foreach (S sitem in s)
				tlist.Add(t(sitem));
			return tlist;
		}

        /// <summary>
        /// 遍历一个枚举器，将指的对象（S）转换成另外一种指定对象（T）（S-->T）
        /// </summary>
        /// <typeparam name="S">需要转换的对象类型</typeparam>
        /// <typeparam name="T">转换后的对象类型</typeparam>
        /// <param name="s">枚举器</param>
        /// <param name="t">操作委托</param>
        /// <param name="count">数组长度</param>
        /// <returns>返回转化后对象类型（T）的list集合</returns>
        /// <remarks>
        /// 如果实际枚举的对象数目小于给定的 count，则数组空余的位置为该类型的缺省值。
		/// 如果实际枚举的对象数目大于给定的 count，则多余的对象被丢弃。
        /// 本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托
        /// </remarks>
		public static T[] Transform<S, T>(IEnumerable<S> s, Func<S, T> t, int count)
		{
			T[] tarray = new T[count];
			int i = 0;
			foreach (S sitem in s)
			{
				if (i >= count)
					break;
				tarray[i++] = t(sitem);
			}
			return tarray;
		}

        #region Find and select

        /// <summary>
        ///  遍历一个集合，指定集合位置查找集合内是否有符合要比较参数（value）的值，T类型需要实现IComparable的接口
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的集合</param>
        /// <param name="pos">遍历的起始位置</param>
        /// <param name="value">需要查找的值</param>
        /// <returns>返回的是查找符合要求的元素在集合中的索引，如果集合中没有符合的元素则返回 -1</returns>
        ///  <remarks>比较的方法为CompareTo，此方法需要实现IComparable来重写</remarks>
        public static int FindIndex<T>(IList<T> s, int pos, T value) where T : IComparable<T>
        {
            for (; pos < s.Count; ++pos)
            {
                if (s[pos].CompareTo(value) == 0)
                    return pos;
            }
            return -1;
        }

        /// <summary>
        /// 遍历一个集合，指定集合位置查找集合内是否有符合的委托操作的值
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的集合</param>
        /// <param name="pos">遍历的起始元素位置</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回的是查找符合要求的元素在集合中的索引，如果集合中没有符合的元素则返回 -1</returns>
        /// <remarks>本方法会遍历给定的集合，对每一个元素调用给定的操作委托</remarks>
		public static int FindIndex<T>(IList<T> s, int pos, Predicate<T> cond)
        {
            for (; pos < s.Count; ++pos)
            {
                if (cond(s[pos]))
                    return pos;
            }
            return -1;
        }

		/// <summary>查找集合中满足条件的第一个元素</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="s">需要遍历的集合</param>
		/// <param name="pos">遍历的起始元素位置</param>
		/// <param name="cond">操作委托</param>
		/// <param name="defv">默认值（可缺省）</param>
		/// <returns>返回符合条件的第一个元素，如果没有返回默认值</returns>
		public static T Find<T>(IList<T> s, int pos, Predicate<T> cond, T defv = default(T))
		{
			for (; pos < s.Count; ++pos)
			{
				var x = s[pos];
				if (cond(x))
					return x;
			}
			return defv;
		}

		/// <summary>
		/// 遍历一个集合，查找集合中所有是否有符合的委托操作的值
		/// </summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="s">需要遍历的集合</param>
		/// <param name="cond">操作委托</param>
		/// <param name="defv">默认值（可缺省）</param>
		/// <returns>返回满足要求的第一个元素，没有没有返回 defv</returns>
		/// <remarks>本方法会遍历给定的集合，对每一个元素调用给定的操作委托</remarks>
		public static T Find<T>(IEnumerable<T> s, Predicate<T> cond, T defv = default(T))
        {
            foreach (var x in s)
			{
				if (cond(x))
					return x;
			}
			return defv;
        }

        /// <summary>
        ///  遍历一个集合，从集合的指定的位置向前遍历查找是否有符合参数（value）的值，T类型需要实现IComparable的接口
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的集</param>
        /// <param name="pos">遍历的起始元素位置</param>
        /// <param name="value">需要查找的值</param>
        /// <returns>返回的是查找符合要求的元素在集合中的索引，如果集合中没有符合的元素则返回 -1</returns>
        /// <remarks>如果指定的位置的参数（pos）小于或等于0，则从集合的最后一个元素开始查找</remarks>
        ///  <remarks>比较的方法为CompareTo，此方法需要实现IComparable来重写</remarks>
        public static int FindLastIndex<T>(IList<T> s, int pos, T value) where T : IComparable<T>
        {
            pos = pos >= 0 ? pos : s.Count - 1;
            while (pos >= 0)
            {
                if (s[pos].CompareTo(value) == 0)
                    return pos;
                --pos;
            }
            return -1;
        }

        /// <summary>
        /// 遍历一个集合，指定集合位置向前查找集合内是否有符合的委托操作的值
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的集合</param>
        /// <param name="pos">遍历的起始元素位置</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回的是查找符合要求的元素在集合中的索引，如果集合中没有符合的元素则返回 -1</returns>
        /// <remarks>
        /// 如果指定的位置的参数（pos）小于或等于0，则从集合的最后一个元素开始查找
        /// 本方法会遍历给定的集合，对每一个元素调用给定的操作委托
        /// </remarks>
		public static int FindLastIndex<T>(IList<T> s, int pos, Predicate<T> cond)
        {
            pos = pos >= 0 ? pos : s.Count - 1;
            while (pos >= 0)
            {
                if (cond(s[pos]))
                    return pos;
                --pos;
            }
            return -1;
        }

        /// <summary>
        /// 遍历一个集合，从集合的最后一个元素开始查找集合内是否有符合的委托操作的值
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的集合</param>
        /// <param name="pos">遍历的起始元素位置</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回最后一个满足条件的元素，如果没有返回 defv</returns>
        /// <remarks>本方法会遍历给定的集合，对每一个元素调用给定的操作委托</remarks>
        public static T FindLast<T>(IList<T> s, int pos, Predicate<T> cond, T defv = default(T))
        {
            int index = FindLastIndex<T>(s, pos, cond);
			return index >= 0 ? s[index] : defv;
        }

        /// <summary>
        /// 遍历一个枚举器，找出其中符合委托操作的元素
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的枚举器</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回遍历的元素中符合操作委托的元素的枚举器</returns>
        /// <remarks>本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托</remarks>
		public static IEnumerable<T> SelectWalk<T>(IEnumerable<T> s, Predicate<T> cond)
        {
            foreach (T x in s)
                if (cond(x))
                    yield return x;
        }

        /// <summary>
        /// 遍历一个枚举器，找出其中符合委托操作的元素
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的枚举器</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回遍历的元素中符合操作委托的元素的list集合</returns>
        /// <remarks>本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托</remarks>
		public static List<T> Select<T>(IEnumerable<T> s, Predicate<T> cond)
        {
            List<T> rs = new List<T>(SelectWalk<T>(s, cond));
            return rs;
        }

        /// <summary>
        /// 遍历一个枚举器，找出其中符合委托操作的元素，并统计符合元素的个数
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="s">需要遍历的枚举器</param>
        /// <param name="cond">操作委托</param>
        /// <returns>返回遍历枚举器中符合操作委托元素的个数</returns>
        /// <remarks>本方法会遍历给定的枚举器，对每一个元素调用给定的操作委托</remarks>
		public static int Count<T>(IEnumerable<T> s, Predicate<T> cond)
        {
            int count = 0;
            foreach (T x in s)
                if (cond(x))
                    ++count;
            return count;
        }

		#endregion

		#region Comparision

		/// <summary>判断两个序列是否完全一致</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="x">序列 1 所在列表容器</param>
		/// <param name="off_x">序列 1 的起始索引</param>
		/// <param name="y">序列 2 所在列表容器</param>
		/// <param name="off_y">序列 2 的起始索引</param>
		/// <param name="count">比较的元素个数</param>
		/// <returns>如果两个序列完全一致返回 true</returns>
		/// <remarks>
		/// 类型 T 需要实现 IComparable 接口用于比较元素。
		/// </remarks>
		public static bool CompareRange<T>(IList<T> x, int off_x, IList<T> y, int off_y, int count) where T : IComparable
		{
			for (int i = 0; i < count; ++i)
				if (x[off_x + i].CompareTo(y[off_y + i]) != 0)
					return false;
			return true;
		}

		/// <summary>判断两个序列是否完全一致</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="x">序列 1 所在列表容器</param>
		/// <param name="off_x">序列 1 的起始索引</param>
		/// <param name="y">序列 2 所在列表容器</param>
		/// <param name="off_y">序列 2 的起始索引</param>
		/// <param name="count">比较的元素个数</param>
		/// <param name="cmp">元素比较器</param>
		/// <returns>如果两个序列完全一致返回 true</returns>
		public static bool CompareRange<T>(IList<T> x, int off_x, IList<T> y, int off_y, int count, Comparison<T> cmp)
		{
			for (int i = 0; i < count; ++i)
				if (cmp(x[off_x + i], y[off_y + i]) != 0)
					return false;
			return true;
		}

		/// <summary>判断两个 IList 集合是否完全一致</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="x">列表 1</param>
		/// <param name="y">列表 2</param>
		/// <returns>如果两个列表集合中的元素完全一致则返回 true</returns>
		/// <remarks>
		/// 完全一致的判断条件为：1 两个集合元素个数一致；2 相同索引的元素一致。
		/// 类型 T 需要实现 IComparable 接口用于比较元素。
		/// </remarks>
		public static bool CompareList<T>(IList<T> x, IList<T> y) where T : IComparable
		{
			if (x.Count != y.Count)
				return false;
			return CompareRange(x, 0, y, 0, x.Count);
		}

		/// <summary>判断两个 IList 集合是否完全一致</summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="x">列表 1</param>
		/// <param name="y">列表 2</param>
		/// <param name="cmp">元素比较器</param>
		/// <returns>如果两个列表集合中的元素完全一致则返回 true</returns>
		/// <remarks>
		/// 完全一致的判断条件为：1 两个集合元素个数一致；2 相同索引的元素一致。
		/// </remarks>
		public static bool CompareList<T>(IList<T> x, IList<T> y, Comparison<T> cmp)
		{
			if (x.Count != y.Count)
				return false;
			return CompareRange(x, 0, y, 0, x.Count, cmp);
		}

		#endregion

		#region Max / min

		/// <summary>获取多个值的最大值</summary>
		/// <typeparam name="T">值类型，必须实现 IComparable 接口</typeparam>
		/// <param name="defv">缺省比较值</param>
		/// <param name="e">值列表</param>
		/// <returns>返回最大值</returns>
		public static T Max<T>(T defv, params T[] e) where T : IComparable
        {
            return Max(defv, (IEnumerable<T>)e);
        }

        /// <summary>获取多个值的最小值</summary>
        /// <remarks>参见 Max 函数。</remarks>
        public static T Min<T>(T defv, params T[] e) where T : IComparable
        {
            return Min(defv, (IEnumerable<T>)e);
        }

        /// <summary>获取一个枚举器表示序列的最大值</summary>
        /// <typeparam name="T">值类型，必须实现 IComparable 接口</typeparam>
        /// <param name="defv">缺省比较值</param>
        /// <param name="e">枚举子</param>
        /// <returns>返回最大值</returns>
        public static T Max<T>(T defv, IEnumerable<T> e) where T : IComparable
        {
            T v = defv;
            foreach (T x in e)
            {
                if (x.CompareTo(v) > 0)
                    v = x;
            }
            return v;
        }

        /// <summary>获取多个值的最小值</summary>
        /// <remarks>参见 Max 函数。</remarks>
        public static T Min<T>(T defv, IEnumerable<T> e) where T : IComparable
        {
            T v = defv;
            foreach (T x in e)
            {
                if (x.CompareTo(v) < 0)
                    v = x;
            }
            return v;
        }

		/// <summary>获取一个枚举器表示序列的最大值</summary>
		/// <typeparam name="T">值类型</typeparam>
		/// <param name="defv">缺省比较值</param>
		/// <param name="e">枚举子</param>
		/// <param name="cmp">比较函数</param>
		/// <returns>返回最大值</returns>
		public static T Max<T>(T defv, IEnumerable<T> e, Comparison<T> cmp)
		{
			T v = defv;
			foreach (T x in e)
			{
				if (cmp(x, v) > 0)
					v = x;
			}
			return v;
		}

		/// <summary>获取一个枚举器表示序列的最小值</summary>
		/// <typeparam name="T">值类型</typeparam>
		/// <param name="defv">缺省比较值</param>
		/// <param name="e">枚举子</param>
		/// <param name="cmp">比较函数</param>
		/// <returns>返回最小值</returns>
		public static T Min<T>(T defv, IEnumerable<T> e, Comparison<T> cmp)
		{
			T v = defv;
			foreach (T x in e)
			{
				if (cmp(x, v) < 0)
					v = x;
			}
			return v;
		}

        #endregion
    }
}
