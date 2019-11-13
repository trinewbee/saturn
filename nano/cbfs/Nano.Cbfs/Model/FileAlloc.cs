using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Nano.Cbfs.Model
{
	public interface IFileAlloc
	{
		object GetId();
		long GetAllocation();
		long GetLength();
	}

	public interface IDiskAlloc
	{
		int GetClusterSize();
		long GetTotalClusters();
		long GetUsedClusters();
		IFileAlloc GetItem(object id);
		IFileAlloc CreateFile();
		void DeleteFile(IFileAlloc alloc);
		long SetAllocation(IFileAlloc alloc, long length);
		void SetLength(IFileAlloc alloc, long length);
		int Read(IFileAlloc alloc, long pos, byte[] data, int offset, int length);
		void Write(IFileAlloc alloc, long pos, byte[] data, int offset, int length);
		void Close();
	}

	class MemoryFileAlloc : IFileAlloc
	{
		internal long m_alloc = 0;
		internal MemoryStream m_stream = new MemoryStream();

		public object GetId() => null;

		public long GetAllocation() => m_alloc;

		public long GetLength() => m_stream != null ? m_stream.Length : 0;

		internal void Close()
		{
			m_alloc = 0;
			if (m_stream != null)
			{
				m_stream.Close();
				m_stream = null;
			}
		}
	}

	public class MemoryDiskAlloc : IDiskAlloc
	{
		public const int ClusterSize = VDisk.SectorSize;
		long m_totelClustors, m_usedClustors;

		public MemoryDiskAlloc()
		{
			m_totelClustors = 0x100000L; // 1MB sectors = 512MB
			m_usedClustors = 0;
		}

		public int GetClusterSize() => ClusterSize;
		public long GetTotalClusters() => m_totelClustors;
		public long GetUsedClusters() => m_usedClustors;

		public IFileAlloc GetItem(object id) { throw new NotImplementedException(); }

		public IFileAlloc CreateFile() => new MemoryFileAlloc();

		public void DeleteFile(IFileAlloc alloc)
		{
			var malloc = (MemoryFileAlloc)alloc;
			long clusters = (malloc.GetAllocation() + ClusterSize - 1) / ClusterSize;
			malloc.Close();

			lock (this)
				m_usedClustors -= clusters;
		}

		public long SetAllocation(IFileAlloc alloc, long length)
		{
			if (length >= 0x20000000)
				return -1;

			var malloc = (MemoryFileAlloc)alloc;
			long oldClusters = (malloc.GetAllocation() + ClusterSize - 1) / ClusterSize;
			long newClusters = (length + ClusterSize - 1) / ClusterSize;

			lock (this)
			{
				long usedClusters = m_usedClustors - oldClusters + newClusters;
				if (usedClusters > m_totelClustors)
					return -1;
				m_usedClustors = usedClusters;
			}

			long newAlloc = newClusters * ClusterSize;
			if (newAlloc > malloc.m_stream.Capacity)
				malloc.m_stream.Capacity = (int)newAlloc; // Capacity of MemoryStream can't be reduced
			malloc.m_alloc = newAlloc;

			return malloc.GetAllocation();
		}

		public void SetLength(IFileAlloc alloc, long length)
		{
			var malloc = (MemoryFileAlloc)alloc;
			Debug.Assert(length <= malloc.m_alloc);
			malloc.m_stream.SetLength(length);
		}

		public int Read(IFileAlloc alloc, long pos, byte[] data, int offset, int length)
		{
			var malloc = (MemoryFileAlloc)alloc;
			var stream = malloc.m_stream;
			stream.Seek(pos, SeekOrigin.Begin);
			return stream.Read(data, offset, length);
		}

		public void Write(IFileAlloc alloc, long pos, byte[] data, int offset, int length)
		{
			var malloc = (MemoryFileAlloc)alloc;
			var stream = malloc.m_stream;
			Debug.Assert(stream.Length >= pos + length);
			stream.Seek(pos, SeekOrigin.Begin);
			stream.Write(data, offset, length);
		}

		public void Close() { }
	}
}
