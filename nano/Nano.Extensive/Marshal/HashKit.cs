using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;
using Nano.Common;

namespace Nano.Ext.Marshal
{
	/// <summary>Hash 辅助类</summary>
	public static class HashKit
	{
		static MD5 m_md5;
		static SHA1 m_sha1;
		static SHA256 m_sha256;
		static SHA384 m_sha384;
		static SHA512 m_sha512;

		static HashKit()
		{
			m_md5 = System.Security.Cryptography.MD5.Create();
			m_sha1 = System.Security.Cryptography.SHA1.Create();
			m_sha256 = System.Security.Cryptography.SHA256.Create();
			m_sha384 = System.Security.Cryptography.SHA384.Create();
			m_sha512 = System.Security.Cryptography.SHA512.Create();
		}

		/// <summary>使用给定的算法计算 Hash</summary>
		/// <param name="alg">算法</param>
		/// <param name="data">待计算数组</param>
		/// <param name="offset">起始位置</param>
		/// <param name="count">字节数</param>
		/// <returns>Hash 值</returns>
		/// <remarks>该函数会对传入的 alg 做 lock 操作</remarks>
		public static BinaryValue Hash(HashAlgorithm alg, byte[] data, int offset, int count)
		{
			byte[] hash_c;
			lock (alg)
			{
				hash_c = alg.ComputeHash(data, offset, count);
			}
			return new BinaryValue(hash_c);
		}

		/// <summary>使用给定的算法计算 Hash</summary>
		/// <param name="alg">算法</param>
		/// <param name="data">待计算数组</param>
		/// <returns>Hash 值</returns>
		/// <remarks>该函数会对传入的 alg 做 lock 操作</remarks>
		public static BinaryValue Hash(HashAlgorithm alg, byte[] data) => Hash(alg, data, 0, data.Length);

		/// <summary>使用给定的算法计算 Hash</summary>
		/// <param name="alg">算法</param>
		/// <param name="stream">数据流</param>
		/// <returns>Hash 值</returns>
		/// <remarks>该函数会对传入的 alg 做 lock 操作</remarks>
		public static BinaryValue Hash(HashAlgorithm alg, Stream stream)
		{
			byte[] hash_c;
			lock (alg)
			{
				hash_c = alg.ComputeHash(stream);
			}
			return new BinaryValue(hash_c);
		}

		/// <summary>使用给定的算法计算给定文件的 Hash</summary>
		/// <param name="alg">算法</param>
		/// <param name="path">文件路径</param>
		/// <returns>Hash 值</returns>
		/// <remarks>该函数会对传入的 alg 做 lock 操作</remarks>
		public static BinaryValue Hash(HashAlgorithm alg, string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return Hash(alg, fs);
		}

		#region MD5

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue MD5(byte[] data, int offset, int count) => Hash(m_md5, data, offset, count);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue MD5(byte[] data) => Hash(m_md5, data);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue MD5(Stream stream) => Hash(m_md5, stream);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue MD5(string path) => Hash(m_md5, path);

		#endregion

		#region SHA1

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA1(byte[] data, int offset, int count) => Hash(m_sha1, data, offset, count);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA1(byte[] data) => Hash(m_sha1, data);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA1(Stream stream) => Hash(m_sha1, stream);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA1(string path) => Hash(m_sha1, path);

		#endregion

		#region SHA256

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA256(byte[] data, int offset, int count) => Hash(m_sha256, data, offset, count);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA256(byte[] data) => Hash(m_sha256, data);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA256(Stream stream) => Hash(m_sha256, stream);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA256(string path) => Hash(m_sha256, path);

		#endregion

		#region SHA384

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA384(byte[] data, int offset, int count) => Hash(m_sha384, data, offset, count);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA384(byte[] data) => Hash(m_sha384, data);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA384(Stream stream) => Hash(m_sha384, stream);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA384(string path) => Hash(m_sha384, path);

		#endregion

		#region SHA512

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA512(byte[] data, int offset, int count) => Hash(m_sha512, data, offset, count);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA512(byte[] data) => Hash(m_sha512, data);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA512(Stream stream) => Hash(m_sha512, stream);

		/// <summary>参见 Hash 函数</summary>
		public static BinaryValue SHA512(string path) => Hash(m_sha512, path);

		#endregion
	}
}
