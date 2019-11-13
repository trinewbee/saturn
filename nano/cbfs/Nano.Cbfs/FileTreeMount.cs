using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Nano.Storage;
using CallbackFS;

namespace Nano.Cbfs
{
	public class FileTreeMountRO : CbfsDisk
	{
		CallbackFileSystem m_cbfs;
		FileTreeItem m_root;
		string m_label;

		public FileTreeMountRO(FileTreeItem root)
		{
			m_cbfs = new CallbackFileSystem();
			m_root = root;
			m_label = "Podo Disk";
		}

		#region File access

		public const char PathSep = '\\';
		public const int SectorSize = 512;

		// Returns null if not found
		public static FileTreeItem FindFile(FileTreeItem item, string path)
		{
			Debug.Assert(path[0] == PathSep);
			int pos = 1, len = path.Length;
			while (pos < len)
			{
				if (!item.IsDir)
					return null;

				int pos2 = path.IndexOf(PathSep, pos);
				if (pos2 < 0)
					pos2 = len;
				string name = path.Substring(pos, pos2 - pos);				

				item = item[name];
				if (item == null)
					return null;
				pos = pos2 < len ? pos2 + 1 : len;
			}
			return item;
		}

		public static WdFileInfo ToFileInfo(FileTreeItem item)
		{
			var info = new WdFileInfo();
			info.Name = item.Name;
			info.Attr = (item.IsDir ? WdFileInfo.FILE_ATTRIBUTE_DIRECTORY : WdFileInfo.FILE_ATTRIBUTE_NORMAL) | WdFileInfo.FILE_ATTRIBUTE_READONLY;
			info.CTime = 
			info.MTime = 
			info.ATime = item.LastWriteTimeUtc;
			info.FileSize = item.Size;
			info.AllocSize = (item.Size + SectorSize - 1) / SectorSize;
			return info;
		}

		FileTreeItem _GetItemFromFileInfo(CbFsFileInfo fileInfo)
		{
			if (fileInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
				return (FileTreeItem)hdl.Target;
			}

			string path = fileInfo.FileName;
			return FindFile(m_root, path);
		}

		#endregion

		#region Cbfs Callback Methods

		CallbackFileSystem CbfsDisk.Cbfs
		{
			get { return m_cbfs; }
		}

		void CbfsDisk.Mount() { }

		void CbfsDisk.Unmount() { }

		uint CbfsDisk.GetVolumeID() { return 0x19800330; }

		string CbfsDisk.GetVolumeLabel() { return m_label; }

		void CbfsDisk.SetVolumeLabel(string label) { m_label = label; }

		void CbfsDisk.GetVolumeSize(out long pTotalNumberOfSectors, out long pNumberOfFreeSectors)
		{
			pTotalNumberOfSectors = 0x40000000 / SectorSize;
			pNumberOfFreeSectors = pTotalNumberOfSectors / 2;
		}

		void CbfsDisk.OpenFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			if ((desiredAccess & WinConst.GENERIC_WRITE) != 0)
				throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);

			var item = FindFile(m_root, path);
			if (item == null)
				throw new ECBFSError(ErrCode.ERROR_FILE_NOT_FOUND);

			Debug.Assert(fileInfo.UserContext == IntPtr.Zero);
			GCHandle hdl = GCHandle.Alloc(item);
			fileInfo.UserContext = GCHandle.ToIntPtr(hdl);
		}

		void CbfsDisk.CreateFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		void CbfsDisk.CloseFile(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			if (fileInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
				hdl.Free();
				fileInfo.UserContext = IntPtr.Zero;
			}
		}

		WdFileInfo CbfsDisk.GetFileInfo(string path)
		{
			var item = FindFile(m_root, path);
			if (item == null)
				return null;

			return ToFileInfo(item);
		}

		List<WdFileInfo> CbfsDisk.EnumerateDirectory(CbFsFileInfo directoryInfo, CbFsHandleInfo handleInfo, string mask)
		{
			string path = directoryInfo.FileName;
			var parent = FindFile(m_root, path);
			if (parent == null)
				throw new ECBFSError(ErrCode.ERROR_PATH_NOT_FOUND);

			List<WdFileInfo> infos = new List<WdFileInfo>();
			WildcardMatch wm = new WildcardMatch();
			wm.Init(mask);

			foreach (var subitem in parent.List())
			{
				if (wm.Match(subitem.Name))
				{
					WdFileInfo info = ToFileInfo(subitem);
					infos.Add(info);
				}
			}

			return infos;
		}

		void CbfsDisk.SetAllocationSize(CbFsFileInfo fileInfo, long allocSize)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		void CbfsDisk.SetEndOfFile(CbFsFileInfo fileInfo, long size)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		void CbfsDisk.SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo, uint attr, DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		bool CbfsDisk.IsDirectoryEmpty(CbFsFileInfo directoryInfo, string path)
		{
			var item = _GetItemFromFileInfo(directoryInfo);
			return item.List().Count == 0;
		}

		bool CbfsDisk.CanFileBeDeleted(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
		{
			return false;
		}

		void CbfsDisk.DeleteFile(CbFsFileInfo fileInfo)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		void CbfsDisk.MoveFile(CbFsFileInfo fileInfo, string NewFileName)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		int CbfsDisk.ReadFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
		{
			var item = _GetItemFromFileInfo(fileInfo);
			byte[] buffer = item.AtomRead(pos, len);
			Debug.Assert(buffer.Length <= len);
			Array.Copy(buffer, 0, data, 0, buffer.Length);
			return buffer.Length;
		}

		int CbfsDisk.WriteFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
		{
			throw new ECBFSError(ErrCode.ERROR_ACCESS_DENIED);
		}

		#endregion

		public void Mount()
		{
			CbfsMounter.Mount(this);
		}

		public void Unmount()
		{
			CbfsMounter.Unmount(this);
		}
	}
}
