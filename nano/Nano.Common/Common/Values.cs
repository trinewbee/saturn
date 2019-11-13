using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Nano.Common
{
	/// <summary>扩展的常用转换函数</summary>
	/// <remarks>
	/// 在 Convert, BitConvert 的基础上增加一些常用的转换函数。
	/// </remarks>
	public static class ExtConvert
	{
		#region Byte array

		/// <summary>将 UInt16 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE16(byte[] buffer, int offset, ushort value)
		{
			buffer[offset + 1] = (byte)value;
			buffer[offset] = (byte)(value >>= 8);
		}

		/// <summary>将 UInt16 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray16(byte[] buffer, int offset, ushort value)
		{
			buffer[offset] = (byte)value;
			buffer[++offset] = (byte)(value >>= 8);
		}

		/// <summary>将 Int16 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE16(byte[] buffer, int offset, short value)
		{
			CopyToArrayBE16(buffer, offset, (ushort)value);
		}

		/// <summary>将 Int16 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray16(byte[] buffer, int offset, short value)
		{
			CopyToArray16(buffer, offset, (ushort)value);
		}

		/// <summary>将 uint 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE(byte[] buffer, int offset, uint value)
		{
			buffer[offset + 3] = (byte)value;
			buffer[offset + 2] = (byte)(value >>= 8);
			buffer[offset + 1] = (byte)(value >>= 8);
			buffer[offset] = (byte)(value >>= 8);
		}

		/// <summary>将 uint 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray(byte[] buffer, int offset, uint value)
		{
			buffer[offset] = (byte)value;
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
		}

		/// <summary>将 int 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE(byte[] buffer, int offset, int value)
		{
			CopyToArrayBE(buffer, offset, (uint)value);
		}

		/// <summary>将 int 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray(byte[] buffer, int offset, int value)
		{
			CopyToArray(buffer, offset, (uint)value);
		}

		/// <summary>将 UInt64 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE64(byte[] buffer, int offset, ulong value)
		{
			buffer[offset + 7] = (byte)value;
			buffer[offset + 6] = (byte)(value >>= 8);
			buffer[offset + 5] = (byte)(value >>= 8);
			buffer[offset + 4] = (byte)(value >>= 8);
			buffer[offset + 3] = (byte)(value >>= 8);
			buffer[offset + 2] = (byte)(value >>= 8);
			buffer[offset + 1] = (byte)(value >>= 8);
			buffer[offset] = (byte)(value >>= 8);
		}

		/// <summary>将 UInt64 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray64(byte[] buffer, int offset, ulong value)
		{
			buffer[offset] = (byte)value;
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
			buffer[++offset] = (byte)(value >>= 8);
		}

		/// <summary>将 Int64 整数写入到字节数组中（Big-endian）</summary>
		public static void CopyToArrayBE64(byte[] buffer, int offset, long value)
		{
			CopyToArrayBE64(buffer, offset, (ulong)value);
		}

		/// <summary>将 Int64 整数写入到字节数组中（Little-endian）</summary>
		public static void CopyToArray64(byte[] buffer, int offset, long value)
		{
			CopyToArray64(buffer, offset, (ulong)value);
		}

		#endregion

		/// <summary>Convert a string to a specified value type</summary>
		/// <param name="vt">Value type</param>
		/// <param name="str">Input string</param>
		/// <returns>Converted object</returns>
		public static object FromString(Type vt, string str)
		{
			if (vt.IsPrimitive)
			{
				switch (vt.FullName)
				{
					case "System.Byte":
						return byte.Parse(str);
					case "System.SByte":
						return sbyte.Parse(str);
					case "System.Int16":
						return short.Parse(str);
					case "System.UInt16":
						return ushort.Parse(str);
					case "System.Int32":
						return int.Parse(str);
					case "System.UInt32":
						return uint.Parse(str);
					case "System.Int64":
						return long.Parse(str);
					case "System.UInt64":
						return ulong.Parse(str);
					case "System.Boolean":
						return bool.Parse(str);
					case "System.Single":
						return float.Parse(str);
					case "System.Double":
						return double.Parse(str);
					default:
						throw new NotSupportedException();
				}
			}
			else if (vt == typeof(string))
				return str;
			else
				throw new NotSupportedException();
		}
	}

	/// <summary>Base16编码</summary>
	/// <remarks>
	/// BinaryValue 类提供了 Base16 编码的基本功能。同时是一个支持比较（二进制数组）的封装类，可以用于需要比较和排序的容器，
	/// 例如 Dictionary, SortedList 等。byte[] 不能直接用于这些容器（做为 Key）。
	/// </remarks>
	public class BinaryValue : IComparable<BinaryValue>
	{
		byte[] m_data;

		/// <summary>构造一个给定长度的全 0 数组 BinaryValue</summary>
		/// <param name="length">数组长度</param>
		public BinaryValue(int length)
		{
			m_data = new byte[length];
		}

		/// <summary>通过给定的数组构造 BinaryValue</summary>
		/// <param name="data">传入的字节数组</param>
		/// <remarks>
		/// 传入的 data 变量将直接被新创建的 BinaryValue 实例使用，因此不要在外部修改该数组中的元素。
		/// </remarks>
		public BinaryValue(byte[] data)
		{
			m_data = data;
		}

		/// <summary>获取是否为一个空数组</summary>
		public bool IsNull
		{
			get { return m_data == null; }
		}

		/// <summary>获取数组的长度</summary>
		/// <remarks>空数组会返回 0</remarks>
		public int Length
		{
			get { return m_data != null ? m_data.Length : 0; }
		}

		/// <summary>获取实例内部的字节数组</summary>
		public byte[] Data
		{
			get { return m_data; }
		}

		/// <summary>获取指定索引的字节</summary>
		/// <param name="index">索引位置</param>
		/// <returns>返回索引为 index 的字节。</returns>
		public byte this[int index]
		{
			get { return m_data[index]; }
		}

		#region Static methods

		/// <summary>根据 Base16 字符串创建实例</summary>
		/// <param name="s">Base16 字符串</param>
		/// <returns>创建的实例</returns>
		public static BinaryValue FromString(string s)
		{
			int len = s.Length;
			if (len == 0)
				return new BinaryValue(null);

			Debug.Assert(len != 0 && (len & 1) == 0);
			len >>= 1;
			byte[] val = new byte[len];
			for (int i = 0; i < len; ++i)
				val[i] = ConvertToByte(s[i + i], s[i + i + 1]);
			return new BinaryValue(val);
		}

		/// <summary>通过字典方法比较两个字节数组</summary>
		/// <param name="lhs">左参数</param>
		/// <param name="rhs">右参数</param>
		/// <returns>1 表示大于，0 表示等于，-1 表示小于</returns>
		/// <remarks>
		/// 该比较按字典法逐字节比较。
		/// null 小于 0 字节数组。
		/// </remarks>
		public static int Compare(byte[] lhs, byte[] rhs)
		{
			if (lhs == rhs)
				return 0;
			if (lhs == null)
				return rhs != null ? -1 : 0;
			else if (rhs == null)
				return 1;

			int n = Math.Min(lhs.Length, rhs.Length);
			for (int i = 0; i < n; ++i)
			{
				int x = lhs[i].CompareTo(rhs[i]);
				if (x != 0)
					return x;
			}
			return lhs.Length.CompareTo(rhs.Length);
		}

		/// <summary>比较两个字节数组是否一致</summary>
		/// <param name="lhs">左参数</param>
		/// <param name="rhs">右参数</param>
		/// <returns>完全一致返回 true</returns>
		/// <remarks>
		/// 该方法进行逐字节比较，只有两个数组的长度和所有字节都一致才返回 true。
		/// null 与 0 字节数组不认为是一致。
		/// </remarks>
		public static bool IsEqual(byte[] lhs, byte[] rhs)
		{
			if (lhs == rhs)
				return true;
			if (lhs == null || rhs == null)
				return false;
			if (lhs.Length != rhs.Length)
				return false;
			for (int i = 0; i < lhs.Length; ++i)
			{
				if (lhs[i] != rhs[i])
					return false;
			}
			return true;
		}

		/// <summary>产生大写的 Base16 字符串</summary>
		/// <param name="data">二进制数据</param>
		/// <param name="off">起始索引</param>
		/// <param name="len">字节数</param>
		/// <returns>大写字符的 Base16 字符串</returns>
		public static string ToHexString(byte[] data, int off, int len)
		{
			string ret = "";
			for (int i = 0; i < len; ++i)
			{
				byte val = data[off + i];
				ret += DigitToChar(val >> 4);
				ret += DigitToChar(val & 0xF);
			}
			return ret;
		}

		/// <summary>产生大写的 Base16 字符串</summary>
		/// <param name="data">二进制数据</param>
		/// <returns>大写字符的 Base16 字符串</returns>
		public static string ToHexString(byte[] data)
		{
			return ToHexString(data, 0, data.Length);
		}

		/// <summary>产生小写的 Base16 字符串</summary>
		/// <param name="data">二进制数据</param>
		/// <param name="off">起始索引</param>
		/// <param name="len">字节数</param>
		/// <returns>小写字符的 Base16 字符串</returns>
		public static string ToHexStringLower(byte[] data, int off, int len)
		{
			return ToHexString(data, off, len).ToLowerInvariant();
		}

		/// <summary>产生小写的 Base16 字符串</summary>
		/// <param name="data">二进制数据</param>
		/// <returns>小写字符的 Base16 字符串</returns>
		public static string ToHexStringLower(byte[] data)
		{
			return ToHexString(data, 0, data.Length).ToLowerInvariant();
		}

		/// <summary>16进制转换为字符</summary>
		/// <param name="v">0-15数值</param>
		/// <returns>16进制字符</returns>
		public static char DigitToChar(int v)
		{
			Debug.Assert((v & 0xF0) == 0);
			return v < 10 ? (char)('0' + v) : (char)('A' + v - 10);
		}

		/// <summary>从16进制字符转为数值</summary>
		/// <param name="c">16进制字符</param>
		/// <returns>对应数值（0-15）</returns>
		public static byte ConvertToDigit(char c)
		{
			if (c >= '0' && c <= '9')
				return (byte)(c - '0');
			else if (c >= 'A' && c <= 'F')
				return (byte)(c - 'A' + 10);
			else if (c >= 'a' && c <= 'f')
				return (byte)(c - 'a' + 10);

			throw new ArgumentException();
		}

		/// <summary>将两个16进制字符转为一个字节</summary>
		/// <param name="fc">高位字符</param>
		/// <param name="sc">低位字符</param>
		/// <returns>对应字节</returns>
		public static byte ConvertToByte(char fc, char sc)
		{
			return (byte)((ConvertToDigit(fc) << 4) | ConvertToDigit(sc));
		}

		#endregion

		/// <summary>返回小写的 Base 16 字符串</summary>
		/// <returns>小写字符的 Base 16 字符串</returns>
		public string ToStringLower()
		{
			if (m_data == null)
				return null;
			return ToHexStringLower(m_data, 0, m_data.Length);
		}

		#region Methods for bases

		/// <summary>判断是否与给定的 obj 相等</summary>
		/// <param name="obj">用于比较的 BinaryValue 实例</param>
		/// <returns>相等时返回 true</returns>
		/// <remarks>参见 IsEqual 方法</remarks>
		public override bool Equals(object obj)
		{
			BinaryValue rhs = (BinaryValue)obj;
			return IsEqual(this.Data, rhs.Data);
		}

		/// <summary>获取哈希值</summary>
		/// <returns>哈希值</returns>
		/// <remarks>
		/// 注意，本方法只是简单返回前 4 个字节表示的整数，因为 BinaryValue 通常本身用于表示一种编码的返回值，其字节值已经离散化。
		/// 因此本函数没有重新实施复杂的哈希运算。
		/// </remarks>
		public override int GetHashCode()
		{
			if (m_data == null || m_data.Length == 0)
				return 0;

			// 只是简单的取了前四个字节
			if (m_data.Length >= 4)
				return BitConverter.ToInt32(m_data, 0);

			var data = new byte[4];
			Array.Copy(m_data, 0, data, 0, m_data.Length);
			return BitConverter.ToInt32(data, 0);
		}

		/// <summary>返回大写的 Base 16 字符串</summary>
		/// <returns>大写字符的 Base 16 字符串</returns>
		public override string ToString()
		{
			if (m_data == null)
				return null;
			return ToHexString(m_data, 0, m_data.Length);
		}

		int IComparable<BinaryValue>.CompareTo(BinaryValue other)
		{
			return Compare(m_data, other != null ? other.m_data : null);
		}

		#endregion
	}

	/// <summary>递归深度比较的辅助工具类</summary>
	public class CmpKit
	{
		public delegate bool Equality<in T>(T a, T b);

		public static bool Eq(int a, int b) { return a == b; }
		public static bool Eq(byte a, byte b) { return a == b; }

		public static bool Eq<T>(T a, T b, Equality<T> eq)
		{
			if (a == null)
				return b == null;
			else if (b == null)
				return false;
			else
				return eq(a, b);
		}

		public static bool ListEq<T>(IList<T> a, IList<T> b, Equality<T> eq)
		{
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; ++i)
			{
				if (!Eq(a[i], b[i], eq))
					return false;
			}
			return true;
		}

		public static bool RangeEq<T>(IList<T> a, int offa, IList<T> b, int offb, int len, Equality<T> eq)
		{
			for (int i = 0; i < len; ++i)
			{
				if (!Eq(a[i + offa], b[i + offb], eq))
					return false;
			}
			return true;
		}

		public static bool DictEq<K, V>(IDictionary<K, V> a, IDictionary<K, V> b, Equality<V> eq)
		{
			if (a.Count != b.Count)
				return false;
			foreach (var pair in a)
			{
				if (!Eq(pair.Value, b[pair.Key], eq))
					return false;
			}
			return true;
		}
	}
}
