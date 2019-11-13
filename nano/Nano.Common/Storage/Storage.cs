using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Nano.Common;

namespace Nano.Storage
{
	public class ObjectInfo
	{
		public string Name;
		public long Size;
	}

	/// <summary>Interface of a key-value storage</summary>
	public interface KeyValueAccess
	{
		#region Properties

		/// <summary>Can create new objects</summary>
		bool CanCreate { get; }

		/// <summary>Can resize an existing object</summary>
		/// <remarks>
		/// If CanAppend is true and CanResize is false, the object can only be truncated but not extended.
		/// </remarks>
		bool CanResize { get; }

		/// <summary>Can open an existing object and append data from the tail</summary>
		bool CanAppend { get; }

		/// <summary>Can modify an existing object</summary>
		bool CanModify { get; }

		/// <summary>Support writable streams</summary>
		/// <remarks>
		/// If SupportStream is false, only atom write apis supported.
		/// </remarks>
		bool SupportStream { get; }

		/// <summary>Whether it is a remote (via network) storage</summary>
		bool IsRemote { get; }

		/// <summary>Max file length supported</summary>
		long MaxSize { get; }

		/// <summary>Saved hash code of objects</summary>
		/// <remarks>
		/// Hash algorithm might be md5,sha1,kuaipan
		/// If no hash code saved, null is returned.
		/// </remarks>
		string[] HashSaved { get; }

		#endregion

		#region Dir methods

		/// <summary>List all objects in a bucket</summary>
		/// <returns>All objects in the bucket</returns>
		List<ObjectInfo> ListObjects();

		ObjectInfo this[string name] { get; }

		ObjectInfo Refresh(string name);

		bool DeleteObject(string name);

		void DeleteAll();

		#endregion

		#region Atom read / write methods

		/// <summary>Read an object</summary>
		/// <param name="name">Object name</param>
		/// <param name="pos">Start position</param>
		/// <param name="size">Size to be read</param>
		/// <returns>Data read</returns>
		byte[] AtomRead(string name, long pos, int size);

		/// <summary>Read an object</summary>
		/// <param name="name">Object name</param>
		/// <returns>Data of the object</returns>
		byte[] AtomRead(string name);

		/// <summary>Write an existing object</summary>
		/// <param name="name">Object name</param>
		/// <param name="pos">Start position</param>
		/// <param name="data">Data</param>
		/// <param name="off">Offset of data</param>
		/// <param name="size">Size of data</param>
		void AtomWrite(string name, long pos, byte[] data, int off, int size);

		void AtomWrite(string name, long pos, Stream istream, int off, int size);

		void AtomWrite(string name, long pos, Stream istream);

		/// <summary>Create an object</summary>
		/// <param name="name">Object name</param>
		/// <param name="data">Data</param>
		/// <param name="off">Offset of data</param>
		/// <param name="size">Size of data</param>
		ObjectInfo AtomCreate(string name, byte[] data, int off, int size);

		ObjectInfo AtomCreate(string name, Stream istream, int off, int size);

		ObjectInfo AtomCreate(string name, byte[] data);

		ObjectInfo AtomCreate(string name, Stream istream);

		void WalkData(string name, StorageKit.AcceptDataDelegate f);

		byte[] ComputeHash(string name, string algorithm);

		#endregion

		#region Stream methods

		/// <summary>Create an object then return a writable stream</summary>
		/// <param name="name">Object name</param>
		/// <param name="size">Size of object</param>
		/// <returns>A writable stream</returns>
		/// <remarks>
		/// The size is the data allocated. If CanResize is true, it can be resized later.
		/// </remarks>
		Tuple<ObjectInfo, Stream> CreateObject(string name, long size);

		/// <summary>Open an existing object then return a stream</summary>
		/// <param name="name">Object name</param>
		/// <param name="writable">Ask for writing or appending</param>
		/// <returns>Stream opened</returns>
		/// <remarks>
		/// If writable is false, a readonly stream is returned.
		/// If writable is true, whether the returned stream supports writing / appending / resizing
		/// depends on CanModify, CanAppend and CanResize properties.
		/// If writable is true, but none of CanModify, CanAppend and CanResize is true,
		/// an AccessDeniedException is thrown.
		/// </remarks>
		Stream OpenObject(string name, bool writable);

        #endregion

        void Close();
	}

	public interface FileTreeItem
	{
		string Name { get; }
		bool IsDir { get; }
		long Size { get; }
		DateTime LastWriteTimeUtc { get; set; }
		FileTreeItem Parent { get; }

		#region Atom read / write methods

		byte[] AtomRead(long pos, int size);

		byte[] AtomRead();

		void AtomWrite(long pos, byte[] data, int off, int size);

		void AtomWrite(long pos, Stream istream, long off, long size);

		void AtomWrite(long pos, Stream istream);

		FileTreeItem AtomCreateChild(string name, byte[] data, int off, int size);

		FileTreeItem AtomCreateChild(string name, Stream istream, long off, long size);

		FileTreeItem AtomCreateChild(string name, byte[] data);

		FileTreeItem AtomCreateChild(string name, Stream istream);

		void WalkData(StorageKit.AcceptDataDelegate f);

		byte[] ComputeHash(string algorithm);

		#endregion

		#region Stream methods

		Tuple<FileTreeItem, Stream> CreateChild(string name, long size);

		Stream Open(bool writable);

		#endregion

		#region Child nodes methods

		List<FileTreeItem> List();

		FileTreeItem this[string name] { get; }

		void Delete(bool recursive);

		void DeleteChildren();

		void MoveTo(FileTreeItem parent, string name);

		FileTreeItem CreateDir(string name);

		void Refresh();

		#endregion
	}

	public interface FileTreeAccess
	{
		bool CanCreate { get; }
		bool CanResize { get; }
		bool CanAppend { get; }
		bool CanModify { get; }

		bool SupportStream { get; }

		bool IsRemote { get; }
		long MaxSize { get; }
		string[] HashSaved { get; }

		FileTreeItem Root { get; }

        void Close();
	}

	public static class StorageKit
	{
        public static int BufferSize = 0x100000;    // 1MB

		public delegate void AcceptDataDelegate(byte[] data, int off, int size);

		public static byte[] AtomRead(Stream istream, long pos, int size)
		{
			long end = Math.Min(istream.Length, pos + size);
			if (end <= pos)
				return new byte[0];

			size = (int)(end - pos);
			byte[] data = new byte[size];
			istream.Seek(pos, SeekOrigin.Begin);
			if (istream.Read(data, 0, size) != size)
				throw new IOException("Read failed");

			return data;
		}

		public static byte[] AtomRead(Stream istream)
		{
			long length = istream.Length;
			if (length >= 0x80000000)
				throw new ArgumentOutOfRangeException();
			return AtomRead(istream, 0, (int)length);
		}

		public static void AtomWrite(Stream ostream, long pos, byte[] data, int off, int size)
		{
			ostream.Seek(pos, SeekOrigin.Begin);
			ostream.Write(data, off, size);
		}

		public static long AtomWrite(Stream ostream, long pos, Stream istream, long off, long size)
		{
			byte[] buffer = new byte[BufferSize];
			istream.Seek(off, SeekOrigin.Begin);
			ostream.Seek(pos, SeekOrigin.Begin);
			long cbtotal = 0;
			while (size > 0)
			{
				int cbread = istream.Read(buffer, 0, buffer.Length);
				if (cbread == 0)
					break;

				if (cbread > size)
					cbread = (int)size;
				ostream.Write(buffer, 0, cbread);
				size -= cbread;
				cbtotal += cbread;
			}
			return cbtotal;
		}

		public static long AtomWrite(Stream ostream, long pos, Stream istream)
		{
			Debug.Assert(istream.CanSeek);
			return AtomWrite(ostream, pos, istream, 0, istream.Length);
		}

		public static void WalkData(Stream istream, AcceptDataDelegate f)
		{
			byte[] buffer = new byte[BufferSize];
			istream.Seek(0, SeekOrigin.Begin);
			int cbread;
			while ((cbread = istream.Read(buffer, 0, buffer.Length)) != 0)
				f(buffer, 0, cbread);
		}

		static HashAlgorithm CreateHash(string algorithm)
		{
			switch (algorithm.ToLowerInvariant())
			{
				case "md5":
					return MD5.Create();
				case "sha1":
				case "sha-1":
					return SHA1.Create();
				default:
					throw new NotSupportedException("Unknown hash algorithm: " + algorithm);
			}
		}

		public static byte[] ComputeHash(byte[] data, int off, int size, string algorithm)
		{
			HashAlgorithm hash = CreateHash(algorithm);
			return hash.ComputeHash(data, off, size);
		}

		public static byte[] ComputeHash(Stream istream, string algorithm)
		{
			HashAlgorithm hash = CreateHash(algorithm);
#if true
			return hash.ComputeHash(istream);
#else
			hash.Initialize();
			WalkData(istream, delegate (byte[] data, int off, int size)
			{
				if (hash.TransformBlock(data, off, size, data, off) != size)
					throw new CryptographicException();
			});
			hash.TransformFinalBlock(new byte[0], 0, 0);
			return hash.Hash;
#endif
		}
	}
}
