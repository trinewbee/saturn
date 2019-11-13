using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Common;
using Nano.Collection;

namespace Nano.Storage.Common
{
	// Entry size 64-byte
	//
	// Header (Entry 0)
	// 00 - 03	DWORD		Mark 53 42 46 01 (SBF\x01)
	// 04 - 07	DWORD		Flags 0
	// 08 - 09	WORD		Page size (4KB recommended)
	// 0A - 0B	WORD		Index pages
	// 0C - 0F	DWORD		Total pages
	// 10 - 3F	BYTE[48]	Reserved
	//
	// Index Entry
	// 00 - 2F	BYTE[48]	Name (UTF-8)
	// 30 - 37	BYTE[8]		Reserved
	// 38 - 3B	DWORD		Size
	// 3C - 3F	DWORD		Starting page
	public class BfsEntry
	{
		public string Name = null;
		public int Index = 0;
		public int Size = 0;
		public int Start = 0;
	}

	public class BfsConfig
	{
		public int PageSize = 0, TotalPages = 0, IndexPages = 0;

		public long BlockSize
		{
			get { return (long)TotalPages * PageSize; }
		}

		public long MaxSize
		{
			get { return (long)(TotalPages - IndexPages) * PageSize; }
		}
	}

	internal class FreeList
	{
		public class Seg
		{
			public int Begin, End;
			public Seg(int b, int e) { Begin = b; End = e; }
			public int InRange(int p)
			{
				if (p < Begin) return -1;
				else if (p < End) return 0;
				else return 1;
			}
		}

		int m_size;
		List<Seg> m_segs;

		public FreeList(int begin, int end)
		{
			m_size = end - begin;
			m_segs = new List<Seg>();
			m_segs.Add(new Seg(begin, end));
		}

		int Seek(int pos)
		{
			// TODO, binary_search
			for (int i = 0; i < m_segs.Count; ++i)
			{
				if (m_segs[i].InRange(pos) <= 0)
					return i;
			}
			return m_segs.Count;
		}

		public void AllocRange(int begin, int end)	// not used
		{
			if (begin >= end)
				throw new ArgumentException();

			int i = Seek(begin);
			Seg it = i != m_segs.Count ? m_segs[i] : null;
			if (i == m_segs.Count || begin < it.Begin || end > it.End)
				throw new IndexOutOfRangeException();

			if (begin > it.Begin)
			{
				if (end < it.End)
				{
					int end2 = it.End;
					it.End = begin;
					m_segs.Insert(++i, new Seg(end, end2));
				}
				else
					it.End = begin;
			}
			else
			{
				if (end < it.End)
					it.Begin = end;
				else
					m_segs.RemoveAt(i);
			}
			m_size -= end - begin;
		}

		public void FreeRange(int begin, int end)
		{
			if (begin >= end)
				throw new ArgumentException();

			int i = Seek(begin);
			Seg it = i != m_segs.Count ? m_segs[i] : null;
			if (i != m_segs.Count && end > it.Begin)
				throw new IndexOutOfRangeException();

			int iPrev = i > 0 ? i - 1 : i;
			Seg itPrev = m_segs[iPrev];
			if (iPrev != i && i != m_segs.Count && begin < itPrev.End)
				throw new IndexOutOfRangeException();

			bool fJoinPrev = iPrev != i && itPrev.End == begin;
			bool fJoinNext = i != m_segs.Count && it.Begin == end;

			if (fJoinPrev)
			{
				if (fJoinNext)
				{
					Debug.Assert(itPrev.End == begin && it.Begin == end);
					itPrev.End = it.End;
					m_segs.RemoveAt(i);
				}
				else
				{
					Debug.Assert(itPrev.End == begin);
					itPrev.End = end;
				}
			}
			else
			{
				if (fJoinNext)
				{
					Debug.Assert(it.Begin == end);
					it.Begin = begin;
				}
				else
					m_segs.Insert(i, new Seg(begin, end));
			}
			m_size += end - begin;
		}

		public Seg AllocSize(int count)
		{
			for (int i = 0; i < m_segs.Count; ++i)
			{
				Seg it = m_segs[i];
				if (it.End - it.Begin >= count)
				{
					Seg seg = new Seg(it.Begin, it.Begin + count);
					Debug.Assert(seg.End <= it.End);
					if (seg.End < it.End)
						it.Begin = seg.End;
					else
						m_segs.RemoveAt(i);

					m_size -= count;
					return seg;
				}
			}
			return null;
		}

		public int SumSize()
		{
			int n = 0;
			foreach (Seg it in m_segs)
				n += it.End - it.Begin;
			return n;
		}

		public int Size
		{
			get { return m_size; }
		}
	}

	public interface BfsBlock
	{
		List<BfsEntry> Entries { get; }

		// Stream methods
		Stream CreateFile(string name, int size, out int entryIndex);
		Stream OpenFile(int entryIndex, bool fWritable);
		void CloseFile(int iEntry);

		// Atom methods
		int CreateObject(byte[] data, int off, int len);
		int ReadObject(int ientry, int pos, byte[] buffer, int offset, int count);
		void WriteObject(int ientry, int pos, byte[] buffer, int offset, int count);

		// Raw methods
		int Read(long pos, byte[] buffer, int offset, int count);
		void Write(long pos, byte[] buffer, int offset, int count);
		void Flush();
	}

	internal class BfsInnerRandomAccessDevice : IFixedSizeRandomAccessDevice
	{
		BfsBlock m_block;
		int m_ientry, m_pbase, m_len;
		bool m_writable;

		internal BfsInnerRandomAccessDevice(BfsBlock block, int ientry, int pbase, int len, bool writable)
		{
			m_block = block;
			m_ientry = ientry;
			m_pbase = pbase;
			m_len = len;
			m_writable = writable;
		}

		public long Length
		{
			get { return m_len; }
		}

		public bool CanWrite
		{
			get { return m_writable; }
		}

		public int Read(long pos, byte[] buffer, int offset, int count)
		{
			return m_block.Read((int)(m_pbase + pos), buffer, offset, count);
		}

		public void Write(long pos, byte[] buffer, int offset, int count)
		{
			if (!m_writable)
				throw new AccessViolationException();
			m_block.Write((int)(m_pbase + pos), buffer, offset, count);
		}

		public void Flush()
		{
			m_block.Flush();
		}

		public void Close()
		{
			m_block.CloseFile(m_ientry);
			m_block = null;
		}
	}

	public class BfsVariantSizeBlock : BfsBlock
	{
		#region Constants

		const int OFF_HDR_MARK = 0,
				OFF_HDR_FLAGS = 4,
				OFF_HDR_PAGESIZE = 8,
				OFF_HDR_IDXPAGES = 0xA,
				OFF_HDR_PAGES = 0xC;

		const int OFF_ENT_UID = 0, OFF_ENT_SIZE = 0x38, OFF_ENT_START = 0x3C;

		const int FILE_MARK = 0x01464253;	// BFS\x01
		const int ENTRY_SIZE = 64;
		const int MAX_NAME_LEN = 48;

		#endregion

		Stream m_stream = null;
		int m_pageSize = 0, m_pages = 0, m_pagesIndex = 0;

		List<BfsEntry> m_entries = null;
		FreeList m_freelist = null;

		static Encoding m_utf8 = Encoding.UTF8;

		public int FreePageCount
		{
			get { return m_freelist.Size; }
		}

		public List<BfsEntry> Entries
		{
			get { return m_entries; }
		}

		internal FreeList Space
		{
			get { return m_freelist; }
		}

		#region Creation

		public void Create(FileTreeItem dir, string name, BfsConfig config)
		{
			var rCreate = dir.CreateChild(name, config.BlockSize);
			m_stream = rCreate.Item2;
			m_pageSize = config.PageSize;
			m_pages = config.TotalPages;
			m_pagesIndex = config.IndexPages;
			int entryCapacity = m_pagesIndex * m_pageSize / ENTRY_SIZE;

			m_stream.SetLength(m_pages * m_pageSize);
			WriteHeader();

			m_entries = new List<BfsEntry>(entryCapacity);
			for (int i = 0; i < entryCapacity; ++i)
				m_entries.Add(null);

			m_freelist = new FreeList(m_pagesIndex, m_pages);
		}

		void WriteHeader()
		{
			byte[] m_buffer = new byte[m_pageSize];
			Array.Clear(m_buffer, 0, m_buffer.Length);

			m_stream.Seek(0, SeekOrigin.Begin);
			for (int i = 0; i < m_pagesIndex; ++i)
				m_stream.Write(m_buffer, 0, m_pageSize);

			m_stream.Seek(0, SeekOrigin.Begin);
			ExtConvert.CopyToArray(m_buffer, OFF_HDR_MARK, FILE_MARK);
			ExtConvert.CopyToArray(m_buffer, OFF_HDR_FLAGS, 0);
			ExtConvert.CopyToArray16(m_buffer, OFF_HDR_PAGESIZE, (ushort)(m_pageSize));
			ExtConvert.CopyToArray16(m_buffer, OFF_HDR_IDXPAGES, (ushort)(m_pagesIndex));
			ExtConvert.CopyToArray(m_buffer, OFF_HDR_PAGES, m_pages);
			m_stream.Write(m_buffer, 0, ENTRY_SIZE);
		}

		#endregion

		#region Open

		public void Open(FileTreeItem file, BfsConfig config)
		{
			m_stream = file.Open(true);

			byte[] buffer = new byte[ENTRY_SIZE];
			if (m_stream.Read(buffer, 0, ENTRY_SIZE) != ENTRY_SIZE)
				throw new IOException();

			ReadHeader(buffer, config);
			m_stream.Seek(0, SeekOrigin.Begin);
			buffer = new byte[m_pageSize];

			for (int i = 0; i < m_pagesIndex; ++i)
			{
				if (m_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
					throw new IOException();

				for (int j = i != 0 ? 0 : ENTRY_SIZE; j < buffer.Length; j += ENTRY_SIZE)
				{
					int pos = Array.IndexOf(buffer, (byte)0, j, MAX_NAME_LEN);
					int namesz = pos >= 0 ? pos - j : MAX_NAME_LEN;
					string name = Encoding.UTF8.GetString(buffer, j, namesz);

					if (name.Length != 0)
					{
						BfsEntry e = new BfsEntry();
						e.Name = name;
						e.Size = BitConverter.ToInt32(buffer, j + OFF_ENT_SIZE);
						e.Start = BitConverter.ToInt32(buffer, j + OFF_ENT_START);
						e.Index = m_entries.Count;
						m_entries.Add(e);

						int pages = (e.Size + m_pageSize - 1) / m_pageSize;
						m_freelist.AllocRange(e.Start, e.Start + pages);
					}
					else
						m_entries.Add(null);
				}
			}
		}

		void ReadHeader(byte[] buffer, BfsConfig config)
		{
			Debug.Assert(BitConverter.ToInt32(buffer, OFF_HDR_MARK) == FILE_MARK);
			Debug.Assert(BitConverter.ToInt32(buffer, OFF_HDR_FLAGS) == 0);

			m_pageSize = BitConverter.ToInt16(buffer, OFF_HDR_PAGESIZE);
			m_pagesIndex = BitConverter.ToInt16(buffer, OFF_HDR_IDXPAGES);
			m_pages = BitConverter.ToInt32(buffer, OFF_HDR_PAGES);
			Debug.Assert(m_pageSize == config.PageSize && m_pages == config.TotalPages && m_pagesIndex == config.IndexPages);
			Debug.Assert(m_pages * m_pageSize == m_stream.Length);

			int entryCount = m_pagesIndex * m_pageSize / ENTRY_SIZE;
			m_entries = new List<BfsEntry>(entryCount);
			m_freelist = new FreeList(m_pagesIndex, m_pages);
			m_entries.Add(null);	// Entry 0 used as header
		}

		#endregion

		public void Close()
		{
			m_stream.Close();
			m_stream = null;
			m_pageSize = m_pages = m_pagesIndex = 0;
		}

		#region Stream methods

		public Stream CreateFile(string name, int size, out int iEntry)
		{
			byte[] nameb = m_utf8.GetBytes(name);
			if (nameb.Length > MAX_NAME_LEN || nameb.Length <= 0)
				throw new ArgumentOutOfRangeException("Length of name out of range");

			iEntry = 1;
			for (; iEntry < m_entries.Count; ++iEntry)
				if (m_entries[iEntry] == null)
					break;
			if (iEntry >= m_entries.Count)
				return null;	// No free entry item

			int pages = (size + m_pageSize - 1) / m_pageSize;
			FreeList.Seg seg = m_freelist.AllocSize(pages);
			if (seg == null)
				return null;	// Not enough free space

			BfsEntry e = new BfsEntry();
			e.Name = name;
			e.Size = size;
			e.Start = seg.Begin;
			m_entries[iEntry] = e;

			byte[] buffer = new byte[ENTRY_SIZE];
			Array.Copy(nameb, 0, buffer, 0, nameb.Length);
			ExtConvert.CopyToArray(buffer, OFF_ENT_SIZE, e.Size);
			ExtConvert.CopyToArray(buffer, OFF_ENT_START, e.Start);
			m_stream.Seek(iEntry * ENTRY_SIZE, SeekOrigin.Begin);
			m_stream.Write(buffer, 0, ENTRY_SIZE);

			BfsInnerRandomAccessDevice device = new BfsInnerRandomAccessDevice(this, iEntry, e.Start * m_pageSize, e.Size, true);
			return new FixedSizeStream(device);
		}

		public Stream OpenFile(int entryIndex, bool fWritable)
		{
			BfsEntry e = m_entries[entryIndex];
			if (e == null)
				return null;

			BfsInnerRandomAccessDevice device = new BfsInnerRandomAccessDevice(this, entryIndex, e.Start * m_pageSize, e.Size, fWritable);
			return new FixedSizeStream(device);
		}

		public void CloseFile(int iEntry) { }

		#endregion

		#region Atom methods

		public int CreateObject(byte[] data, int off, int len)
		{
			throw new NotImplementedException();
		}

		public int ReadObject(int ientry, int pos, byte[] buffer, int offset, int count)
		{
			BfsEntry entry = m_entries[ientry];
			if (entry == null)
				throw new KeyNotFoundException();
			count = Math.Min(entry.Size - pos, count);
			if (count <= 0)
				return 0;
			return Read((long)entry.Start * m_pageSize + pos, buffer, offset, count);
		}

		public void WriteObject(int ientry, int pos, byte[] buffer, int offset, int count)
		{
			BfsEntry entry = m_entries[ientry];
			if (entry == null)
				throw new KeyNotFoundException();
			if (pos + count > entry.Size)
				throw new ArgumentOutOfRangeException();
			Write((long)entry.Start * m_pageSize + pos, buffer, offset, count);
		}

		#endregion

		#region Raw methods

		public int Read(long pos, byte[] buffer, int offset, int count)
		{
			lock (this)
			{
				m_stream.Seek(pos, SeekOrigin.Begin);
				return m_stream.Read(buffer, offset, count);
			}
		}

		public void Write(long pos, byte[] buffer, int offset, int count)
		{
			lock (this)
			{
				m_stream.Seek(pos, SeekOrigin.Begin);
				m_stream.Write(buffer, offset, count);
			}
		}

		public void Flush()
		{
			m_stream.Flush();
		}

		#endregion
	}
}
