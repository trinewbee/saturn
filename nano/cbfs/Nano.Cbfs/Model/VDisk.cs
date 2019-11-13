using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CallbackFS;

namespace Nano.Cbfs.Model
{
	public class VDisk : CbfsDisk
	{
		public const int SectorSize = 512;

		IDiskAlloc m_alloc;
		IFileModel m_ft;
		CallbackFileSystem m_cbfs;
		int m_clusterSize, m_clusterSecs;

		public VDisk(IDiskAlloc alloc, IFileModel ft)
		{
			m_alloc = alloc;
			m_ft = ft;
			m_cbfs = new CallbackFileSystem();
			Debug.Assert(m_cbfs.SectorSize == SectorSize);
			m_cbfs.SectorSize = SectorSize;

			m_clusterSize = m_alloc.GetClusterSize();
			Debug.Assert(m_clusterSize >= SectorSize && m_clusterSize % SectorSize == 0);
			m_clusterSecs = m_clusterSize / SectorSize;
			m_cbfs.ClusterSize = (uint)m_clusterSize;
		}

		public CallbackFileSystem Cbfs
		{
			get { return m_cbfs; }
		}

		public void Mount() { }
		public void Unmount() { }

		public void GetVolumeSize(out long pTotalNumberOfSectors, out long pNumberOfFreeSectors)
		{
			pTotalNumberOfSectors = m_alloc.GetTotalClusters() * m_clusterSecs;
			pNumberOfFreeSectors = pTotalNumberOfSectors - m_alloc.GetUsedClusters() * m_clusterSecs;
		}

		public string GetVolumeLabel() => m_ft.GetLabel();
		public void SetVolumeLabel(string label) => m_ft.SetLabel(label);
		public uint GetVolumeID() => m_ft.GetVolumeId();

		public void CreateFile(string path, uint desiredAccess, uint attrs, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			string name;
			var parent = m_ft.FindParentDirectory(path, out name);
			if (parent == null)
				throw new ECBFSError(ErrCode.ERROR_INVALID_PARAMETER);
			if (parent.GetChild(name) != null)
				throw new ECBFSError(ErrCode.ERROR_ALREADY_EXISTS);

			IFileAlloc alloc = null;
			if (WdFileInfo.IsAttrFile(attrs))
				alloc = m_alloc.CreateFile();
			var fi = m_ft.CreateFile(parent, name, attrs, alloc);

			Debug.Assert(fileInfo.UserContext == IntPtr.Zero);
			GCHandle hdl = GCHandle.Alloc(fi);
			fileInfo.UserContext = GCHandle.ToIntPtr(hdl);
		}

		public void OpenFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			if (fileInfo.UserContext != IntPtr.Zero)
				return;

			var item = m_ft.FindFile(path);
			if (item == null)
				throw new ECBFSError(ErrCode.ERROR_FILE_NOT_FOUND);

			Debug.Assert(fileInfo.UserContext == IntPtr.Zero);
			GCHandle hdl = GCHandle.Alloc(item);
			fileInfo.UserContext = GCHandle.ToIntPtr(hdl);
		}

		public void CloseFile(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			if (fileInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
				hdl.Free();
				fileInfo.UserContext = IntPtr.Zero;
			}
		}

		public WdFileInfo GetFileInfo(string path)
		{
			var item = m_ft.FindFile(path);
			if (item == null)
				return null;

			return item.ToFileInfo();
		}

		public List<WdFileInfo> EnumerateDirectory(CbFsFileInfo directoryInfo, CbFsHandleInfo handleInfo, string mask)
		{
			string path = directoryInfo.FileName;
			var parent = m_ft.FindFile(path);
			if (parent == null)
				throw new ECBFSError(ErrCode.ERROR_PATH_NOT_FOUND);

			List<WdFileInfo> infos = new List<WdFileInfo>();
			WildcardMatch wm = new WildcardMatch();
			wm.Init(mask);

			foreach (var subitem in parent.List())
			{
				if (wm.Match(subitem.GetName()))
				{
					WdFileInfo info = subitem.ToFileInfo();
					infos.Add(info);
				}
			}

			return infos;
		}

		public void SetAllocationSize(CbFsFileInfo fileInfo, long allocSize)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			var alloc = item.GetAlloc();
			if (m_alloc.SetAllocation(alloc, allocSize) < 0)
				throw new ECBFSError(ErrCode.ERROR_DISK_FULL);
		}

		public void SetEndOfFile(CbFsFileInfo fileInfo, long size)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			var fa = item.GetAlloc();
			Debug.Assert(fa.GetAllocation() >= size);
			m_alloc.SetLength(fa, size);
		}

		// DateTime.MinValue means do not update
		public void SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo, uint attrs,
			DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			if (attrs != 0)
				m_ft.UpdateAttrs(item, attrs);
			m_ft.UpdateTime(item, creationTime.ToUniversalTime(), lastWriteTime.ToUniversalTime(), lastAccessTime.ToUniversalTime());
		}

		public bool CanFileBeDeleted(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			return true;
		}

		public void DeleteFile(CbFsFileInfo fileInfo)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			m_ft.RemoveFile(item);

			if (!item.IsDir())
				m_alloc.DeleteFile(item.GetAlloc());

			if (fileInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
				hdl.Free();
				fileInfo.UserContext = IntPtr.Zero;
			}
		}

		public void MoveFile(CbFsFileInfo fileInfo, string NewFileName)
		{
			var item = _GetItemFromFileInfo(fileInfo);

			string name;
			var parentNew = m_ft.FindParentDirectory(NewFileName, out name);
			if (parentNew == null)
				throw new ECBFSError(ErrCode.ERROR_INVALID_PARAMETER);

			m_ft.MoveFile(item, parentNew, name);
		}

		public int ReadFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			var fa = item.GetAlloc();
			return m_alloc.Read(fa, pos, data, 0, len);
		}

		public int WriteFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			var fa = item.GetAlloc();
			long newSize = pos + len;
			if (newSize > fa.GetLength())
			{
				if (newSize > fa.GetAllocation())
					this.SetAllocationSize(fileInfo, newSize);
				this.SetEndOfFile(fileInfo, newSize);
			}

			Debug.Assert(pos + len <= fa.GetLength());
			m_alloc.Write(fa, pos, data, 0, len);
			var dt = DateTime.UtcNow;
			return len;
		}

		public bool IsDirectoryEmpty(CbFsFileInfo directoryInfo, string path)
		{
			var item = _GetItemFromFileInfo(directoryInfo);
			return item.IsEmpty();
		}

		IFileItem _GetItemFromFileInfo(CbFsFileInfo fileInfo)
		{
			if (fileInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
				return (IFileItem)hdl.Target;
			}

			string path = fileInfo.FileName;
			return m_ft.FindFile(path);
		}
	}
}
