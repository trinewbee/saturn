using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Common;

namespace Nano.Storage.Backward.Bfs
{
	// Config of simple block
	// Block size: 64MB
	// Index size: 32KB
	// Page size: 4KB
	//
	// Index entry size: 32B
	// Number of entries: 1023 (Entry 0 for header)
	// 
	// Header (Entry 0)
	// 00 - 03	DWORD		Mark 53 42 46 00 (SBF\0)
	// 04 - 07	DWORD		Flags 0
	// 08 - 09	WORD		Page size 0x1000 (4KB)
	// 0A - 0B	WORD		Index pages 8 (32KB)
	// 0C - 0F	DWORD		Total pages 0x4000 (64MB)
	// 10 - 1F	BYTE[16]	Reserved
	//
	// Index Entry
	// 00 - 17	BYTE[24]	UID (All 0 for unused entry item)
	// 18 - 1B	DWORD		Size
	// 1C - 1F	DWORD		Starting page

	public class Entry
	{
		public BinaryValue UID = null;
		public int Size = 0;
		public int Start = 0;
		public int Index = 0;

		public static bool IsUnused(byte[] uid)
		{
			foreach (byte s in uid)
				if (s != 0)
					return false;
			return true;
		}

		public static bool IsUnused(BinaryValue uid)
		{
			return IsUnused(uid.Data);
		}

		public bool IsUnused()
		{
			return IsUnused(UID);
		}
	}

	public class FreeList
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

	public interface Block
	{
		Stream CreateFile(BinaryValue uid, int size, out int entryIndex);
		Stream OpenFile(int entryIndex, bool fWritable);
		IList<Entry> Entries { get; }

		int Read(int pos, byte[] buffer, int offset, int count);
		void Write(int pos, byte[] buffer, int offset, int count);
		void Flush();
		void CloseFile(int iEntry);
	}

	internal class InnerRandomAccessDevice : IFixedSizeRandomAccessDevice
	{
		Block m_block;
		int m_ientry, m_pbase, m_len;
		bool m_writable;

		internal InnerRandomAccessDevice(Block block, int ientry, int pbase, int len, bool writable)
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

	public class BlockStorageConfig
	{
		public int PageSize, TotalPages, IndexPages;

		public BlockStorageConfig(int _pageSize, int _totalPages, int _indexPages)
		{
			PageSize = _pageSize;
			TotalPages = _totalPages;
			IndexPages = _indexPages;
		}

		public static BlockStorageConfig Make64M()
		{
			// Page size 4KB
			// Total size 64MB
			// Index size 32KB
			return new BlockStorageConfig(0x1000, 0x4000, 8);
		}
	}

	public class VariantSizeBlock: Block
	{
		#region Constants

		const int OFF_HDR_MARK = 0,
				OFF_HDR_FLAGS = 4,
				OFF_HDR_PAGESIZE = 8,
				OFF_HDR_IDXPAGES = 0xA,
				OFF_HDR_PAGES = 0xC;

		const int OFF_ENT_UID = 0, OFF_ENT_SIZE = 0x18, OFF_ENT_START = 0x1C;

		const int FILE_MARK = 0x00464253;
		const int ENTRY_SIZE = 32;

		#endregion

		Stream m_stream = null;
		int m_pageSize = 0, m_pages = 0, m_pagesIndex = 0;

		List<Entry> m_entries = null;
		FreeList m_freelist = null;

		#region Creation

		public void Create(string path, BlockStorageConfig config)
		{
			m_stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
			m_pageSize = config.PageSize;
			m_pages = config.TotalPages;
			m_pagesIndex = config.IndexPages;
			int entryCapacity = m_pagesIndex * m_pageSize / ENTRY_SIZE;

			m_stream.SetLength(m_pages * m_pageSize);
			WriteHeader();
			
			m_entries = new List<Entry>(entryCapacity);
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

		public void Open(string path, BlockStorageConfig config)
		{
			m_stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			
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
					byte[] uid_raw = new byte[24];
					Array.Copy(buffer, j, uid_raw, 0, 24);

					if (Entry.IsUnused(uid_raw))
					{
						m_entries.Add(null);
						continue;
					}

					Entry e = new Entry();
					e.UID = new BinaryValue(uid_raw);
					e.Size = BitConverter.ToInt32(buffer, j + 24);
					e.Start = BitConverter.ToInt32(buffer, j + 28);
					e.Index = m_entries.Count;
					m_entries.Add(e);

					int pages = (e.Size + m_pageSize - 1) / m_pageSize;
					m_freelist.AllocRange(e.Start, e.Start + pages);
				}
			}
		}

		void ReadHeader(byte[] buffer, BlockStorageConfig config)
		{
			Debug.Assert(BitConverter.ToInt32(buffer, OFF_HDR_MARK) == FILE_MARK);
			Debug.Assert(BitConverter.ToInt32(buffer, OFF_HDR_FLAGS) == 0);

			m_pageSize = BitConverter.ToInt16(buffer, OFF_HDR_PAGESIZE);
			m_pagesIndex = BitConverter.ToInt16(buffer, OFF_HDR_IDXPAGES);
			m_pages = BitConverter.ToInt32(buffer, OFF_HDR_PAGES);
			Debug.Assert(m_pageSize == config.PageSize && m_pages == config.TotalPages && m_pagesIndex == config.IndexPages);
			Debug.Assert(m_pages * m_pageSize == m_stream.Length);

			int entryCount = m_pagesIndex * m_pageSize / ENTRY_SIZE;
			m_entries = new List<Entry>(entryCount);
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

		public Stream CreateFile(BinaryValue uid, int size, out int iEntry)
		{
			if (uid.Length != 24)
				throw new ArgumentException("Invalid UID size");

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

			Entry e = new Entry();
			e.UID = uid;
			e.Size = size;
			e.Start = seg.Begin;
			m_entries[iEntry] = e;

			byte[] buffer = new byte[ENTRY_SIZE];
			Array.Copy(e.UID.Data, 0, buffer, 0, e.UID.Length);
			ExtConvert.CopyToArray(buffer, 24, e.Size);
			ExtConvert.CopyToArray(buffer, 28, e.Start);
			m_stream.Seek(iEntry * ENTRY_SIZE, SeekOrigin.Begin);
			m_stream.Write(buffer, 0, ENTRY_SIZE);

			InnerRandomAccessDevice device = new InnerRandomAccessDevice(this, iEntry, e.Start * m_pageSize, e.Size, true);
			return new FixedSizeStream(device);
		}

		public Stream OpenFile(int entryIndex, bool fWritable)
		{
			Entry e = m_entries[entryIndex];
			if (e == null)
				return null;

			InnerRandomAccessDevice device = new InnerRandomAccessDevice(this, entryIndex, e.Start * m_pageSize, e.Size, fWritable);
			return new FixedSizeStream(device);
		}

		public int Read(int pos, byte[] buffer, int offset, int count)
		{
			lock (this)
			{
				m_stream.Seek(pos, SeekOrigin.Begin);
				return m_stream.Read(buffer, offset, count);
			}
		}

		public void Write(int pos, byte[] buffer, int offset, int count)
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

		public void CloseFile(int iEntry) { }

		public IList<Entry> Entries
		{
			get { return m_entries; }
		}

		public FreeList Space
		{
			get { return m_freelist; }
		}
	}

	public class FixSizeBlock
	{
		
	}

	public class BlockStorage : IKeyValueStorage
	{
		public class FileItem
		{
			public BinaryValue Uid;
			public Block Block;
			public int EntryIndex;

			public FileItem(BinaryValue uid, Block block, int entryIndex)
			{
				Uid = uid;
				Block = block;
				EntryIndex = entryIndex;
			}
		}

		BlockStorageConfig m_config = null;
		string m_pathHome = null;
		List<VariantSizeBlock> m_vsbArray = null;
		Dictionary<BinaryValue, FileItem> m_files = null;

		#region Open

		public void Open(string path, BlockStorageConfig config)
		{
			m_config = config;
			m_pathHome = path;
			m_vsbArray = new List<VariantSizeBlock>();
			m_files = new Dictionary<BinaryValue, FileItem>();

			string[] pathBlocks = Directory.GetFiles(m_pathHome);
			Array.Sort(pathBlocks);
			foreach (string pathBlock in pathBlocks)
			{
				string name = Path.GetFileName(pathBlock);
				switch (name[0])
				{
					case 'V':
						OpenVsBlock(pathBlock);
						break;
					default:
						throw new IOException("Unknown block file");
				}
			}
		}

		public void Open(string path, BlockStorageConfig config, bool fCreate)
		{
			if (fCreate)
				Directory.CreateDirectory(path);

			Open(path, config);
		}

		void OpenVsBlock(string pathBlock)
		{
			string name = Path.GetFileName(pathBlock);
			int idx = Convert.ToInt32(name.Substring(1));
			while (m_vsbArray.Count <= idx)
				m_vsbArray.Add(null);

			VariantSizeBlock block = new VariantSizeBlock();
			block.Open(pathBlock, m_config);
			m_vsbArray[idx] = block;

			IList<Entry> entries = block.Entries;
			for (int i = 0; i < entries.Count; ++i)
			{
				Entry e = entries[i];
				if (e != null)
					AddItem(block, e);
			}
		}

		void AddItem(Block block, Entry e)
		{
			FileItem fi = new FileItem(e.UID, block, e.Index);
			m_files.Add(e.UID, fi);
		}

		public void Close()
		{
			m_files.Clear();
			foreach (VariantSizeBlock block in m_vsbArray)
				block.Close();
			m_vsbArray = null;
			m_pathHome = null;
		}

		#endregion

		#region IKeyValueStorage

		public RewriteCapability RewriteCaps
		{
			get { return RewriteCapability.CanWrite; }
		}

		public IEnumerable<BinaryValue> ListKeys()
		{
			return m_files.Keys;
		}

		public bool HasKey(BinaryValue key)
		{
			return m_files.ContainsKey(key);
		}

		public Stream CreateFile(BinaryValue key, long size)
		{
			if (key.Length == 0 || key.Length > 24)
				throw new ArgumentException("Invalid UID");
			if (size > 0x2000000 || size <= 0)	// 32MB
				throw new ArgumentOutOfRangeException("Unsupported file size");

			foreach (Block block in m_vsbArray)
			{
				int iEntry;
				Stream stream = block.CreateFile(key, (int)size, out iEntry);
				if (stream != null)
				{
					m_files.Add(key, new FileItem(key, block, iEntry));
					return stream;
				}
			}

			// if (true)
			{
				VariantSizeBlock newblock = CreateNewVsBlock();
				int iEntry;
				Stream stream = newblock.CreateFile(key, (int)size, out iEntry);
				m_files.Add(key, new FileItem(key, newblock, iEntry));
				return stream;
			}
		}

		public Stream OpenFile(BinaryValue key, DataOpenMode mode)
		{
			if (mode != DataOpenMode.Read && mode != DataOpenMode.ReadWrite)
				throw new AccessViolationException();

			FileItem fi = m_files[key];
			if (fi == null)
				throw new FileNotFoundException();

			return fi.Block.OpenFile(fi.EntryIndex, mode == DataOpenMode.ReadWrite);
		}

		#endregion

		#region Inner operations

		VariantSizeBlock CreateNewVsBlock()
		{
			int i = 0;
			for (; i < m_vsbArray.Count; ++i)
				if (m_vsbArray[i] == null)
					break;
			if (i == m_vsbArray.Count)
				m_vsbArray.Add(null);

			VariantSizeBlock block = new VariantSizeBlock();
			block.Create(Path.Combine(m_pathHome, "V" + i.ToString()), m_config);
			return m_vsbArray[i] = block;
		}

		#endregion

		#region Diagnostics

		public List<VariantSizeBlock> VsbList
		{
			get { return m_vsbArray; }
		}

		public IEnumerable<BinaryValue> List()
		{
			return m_files.Keys;
		}

		public void PrintInformation(TextWriter tw)
		{
			tw.WriteLine("BlockStorage");
			tw.WriteLine("Page size = {0}KB, block size = {1}MB, index size = {2}KB",
				m_config.PageSize >> 10,
				(m_config.TotalPages * m_config.PageSize) >> 20,
				(m_config.IndexPages * m_config.PageSize) >> 10
				);
			tw.WriteLine("Variant size blocks");
			tw.WriteLine("\tEntries\t\tPages\t\tSize");
			for (int i = 0; i < m_vsbArray.Count; ++i)
			{
				VariantSizeBlock vsb = m_vsbArray[i];
				IList<Entry> entries = vsb.Entries;

				int usedEntries = 0;
				foreach (Entry entry in entries)
					if (entry != null && !entry.IsUnused())
						++usedEntries;

				FreeList space = vsb.Space;
				int pgsData = m_config.TotalPages - m_config.IndexPages;
				int pgsUsed = pgsData - space.Size;

				tw.WriteLine("{0}\t{1}/{2}\t{3}/{4}\t{5}/{6}(MB)",
					i, usedEntries, entries.Count - 1,
					pgsUsed, pgsData,
					pgsUsed * m_config.PageSize >> 20, pgsData * m_config.PageSize >> 20
					);
			}
		}

		#endregion
	}

	public class HybridBlockStorageConfig
	{
		public BlockStorageConfig bsc = null;
		public int diffSize = 0;
		public IKeyLocator kloc = null;
	}

	public class HybridBlockStorage : IKeyValueStorage
	{
		int m_diffSize = 0;
		string m_pathHome = null, m_pathBlk = null, m_pathLbs = null;
		BlockStorage m_blkStor = null;
		LocalKeyValueStorage m_lbsStor = null;

		#region Open, close

		public void Open(string path, HybridBlockStorageConfig config, bool fCreate)
		{
			m_pathHome = path;
			if (fCreate)
				Directory.CreateDirectory(m_pathHome);

			m_pathBlk = Path.Combine(m_pathHome, "blk");
			m_blkStor = new BlockStorage();
			m_blkStor.Open(m_pathBlk, config.bsc, fCreate);

			m_pathLbs = Path.Combine(m_pathHome, "lbs");
			m_lbsStor = new LocalKeyValueStorage(config.kloc);
			m_lbsStor.Open(m_pathLbs, fCreate);

			m_diffSize = config.diffSize;
		}

		public void Close()
		{
			m_blkStor.Close();
			m_blkStor = null;
			m_lbsStor = null;
			m_pathHome = m_pathBlk = m_pathLbs = null;
		}

		#endregion

		#region IKeyValueStorage

		public RewriteCapability RewriteCaps
		{
			get { return RewriteCapability.AtomBehavior; }
		}

		public IEnumerable<BinaryValue> ListKeys()
		{
			foreach (var key in m_blkStor.ListKeys())
				yield return key;
			foreach (var key in m_lbsStor.ListKeys())
				yield return key;
		}

		public bool HasKey(BinaryValue key)
		{
			return m_blkStor.HasKey(key) || m_lbsStor.HasKey(key);
		}

		public Stream CreateFile(BinaryValue key, long size)
		{
			if (size <= m_diffSize)
				return m_blkStor.CreateFile(key, size);
			else
				return m_lbsStor.CreateFile(key, size);
		}

		public Stream OpenFile(BinaryValue key, DataOpenMode mode)
		{
			if (m_blkStor.HasKey(key))
				return m_blkStor.OpenFile(key, mode);
			else
				return m_lbsStor.OpenFile(key, mode);
		}

		#endregion

		public BlockStorage BlockStor
		{
			get { return m_blkStor; }
		}

		public LocalKeyValueStorage LargeStor
		{
			get { return m_lbsStor; }
		}

		public void PrintInformation(TextWriter tw)
		{
			m_blkStor.PrintInformation(tw);
		}
	}
}
