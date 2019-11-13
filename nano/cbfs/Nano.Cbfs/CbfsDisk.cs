using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using CallbackFS;

namespace Nano.Cbfs
{
	public static class ErrCode
	{
		public const uint ERROR_FILE_NOT_FOUND = 2;
		public const uint ERROR_PATH_NOT_FOUND = 3;
		public const uint ERROR_ACCESS_DENIED = 5;

		public const uint ERROR_INVALID_PARAMETER = 87;

		public const uint ERROR_DISK_FULL = 112;

		public const uint ERROR_ALREADY_EXISTS = 183;
	}

	public static class WinConst
	{
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;

	}

	public class WdFileInfo
	{
		public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
		public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
		public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
		public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
		public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
		public const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
		public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

		public string Name;
		public uint Attr;
		public DateTime CTime, MTime, ATime;
		public long FileSize, AllocSize;

		public static bool IsAttrFile(uint attr) => (attr & (FILE_ATTRIBUTE_DEVICE | FILE_ATTRIBUTE_DIRECTORY)) == 0;
	}

	public interface CbfsDisk
	{
		CallbackFileSystem Cbfs { get; }

		void Mount();
		void Unmount();
		void GetVolumeSize(out long pTotalNumberOfSectors, out long pNumberOfFreeSectors);
		string GetVolumeLabel();
		void SetVolumeLabel(string label);
		uint GetVolumeID();
		void CreateFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo);
		void OpenFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo);
		void CloseFile(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo);
		WdFileInfo GetFileInfo(string path);
		List<WdFileInfo> EnumerateDirectory(CbFsFileInfo directoryInfo, CbFsHandleInfo handleInfo, string mask);
		void SetAllocationSize(CbFsFileInfo fileInfo, long allocSize);
		void SetEndOfFile(CbFsFileInfo fileInfo, long size);
		void SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo, uint attr,
			DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime);
		bool CanFileBeDeleted(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo);
		void DeleteFile(CbFsFileInfo fileInfo);
		void MoveFile(CbFsFileInfo fileInfo, string NewFileName);
		int ReadFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len);
		int WriteFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len);
		bool IsDirectoryEmpty(CbFsFileInfo directoryInfo, string path);
	}

	class WdEnumCtx
	{
		internal List<WdFileInfo> Items;
		internal int Index;
	}

	static class CbfsProxy
	{
		static TextWriter Log = false ? Console.Out : TextWriter.Null;

		internal static void CbfsMount(CallbackFileSystem sender)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.Mount();
		}

		internal static void CbfsUnmount(CallbackFileSystem sender)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.Unmount();
		}

		internal static void CbfsGetVolumeSize(CallbackFileSystem sender, ref long TotalNumberOfSectors, ref long NumberOfFreeSectors)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.GetVolumeSize(out TotalNumberOfSectors, out NumberOfFreeSectors);
		}

		internal static void CbfsGetVolumeLabel(CallbackFileSystem sender, ref string VolumeLabel)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			VolumeLabel = disk.GetVolumeLabel();
		}

		internal static void CbfsSetVolumeLabel(CallbackFileSystem sender, string VolumeLabel)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.SetVolumeLabel(VolumeLabel);
		}

		internal static void CbfsGetVolumeId(CallbackFileSystem sender, ref uint VolumeID)
		{
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			VolumeID = disk.GetVolumeID();
		}

		internal static void CbfsCreateFile(CallbackFileSystem sender, string FileName, uint DesiredAccess, uint FileAttributes, uint ShareMode, CbFsFileInfo FileInfo, CbFsHandleInfo HandleInfo)
		{
			Log.WriteLine("[CreateFile] path={0}", FileName);
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.CreateFile(FileName, DesiredAccess, FileAttributes, ShareMode, FileInfo, HandleInfo);
		}

		internal static void CbfsOpenFile(CallbackFileSystem sender, string FileName, uint DesiredAccess, uint FileAttributes, uint ShareMode, CbFsFileInfo FileInfo, CbFsHandleInfo HandleInfo)
		{
			Log.WriteLine("[OpenFile] path={0}", FileName);
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.OpenFile(FileName, DesiredAccess, FileAttributes, ShareMode, FileInfo, HandleInfo);
		}

		internal static void CbfsCloseFile(CallbackFileSystem sender, CbFsFileInfo FileInfo, CbFsHandleInfo HandleInfo)
		{
			Log.WriteLine("[CloseFile] path={0}", FileInfo.FileName);
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.CloseFile(FileInfo, HandleInfo);
		}

		internal static void CbfsGetFileInfo(CallbackFileSystem sender, string FileName, ref bool FileExists,
			ref DateTime CreationTime, ref DateTime LastAccessTime, ref DateTime LastWriteTime, ref DateTime ChangeTime,
			ref long EndOfFile, ref long AllocationSize, ref long FileId, ref uint FileAttributes,
			ref uint NumberOfLinks, ref string ShortFileName, ref string RealFileName)
		{
			Log.WriteLine("[GetFileInfo] path={0}", FileName);
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			WdFileInfo info = disk.GetFileInfo(FileName);
			if (info != null)
			{
				FileExists = true;
				CreationTime = info.CTime;
				LastAccessTime = info.ATime;
				LastWriteTime = info.MTime;
				EndOfFile = info.FileSize;
				AllocationSize = info.AllocSize;
				FileId = 0;
				FileAttributes = info.Attr;
				Log.WriteLine("file exists");
			}
			else
			{
				FileExists = false;
				Log.WriteLine("file not found");
			}
		}

		internal static void CbfsEnumerateDirectory(CallbackFileSystem Sender,
			CbFsFileInfo DirectoryInfo, CbFsHandleInfo HandleInfo, CbFsDirectoryEnumerationInfo EnumerationInfo,
			string Mask, int Index, bool Restart,
			ref bool FileFound, ref string FileName, ref string ShortFileName,
			ref DateTime CreationTime, ref DateTime LastAccessTime, ref DateTime LastWriteTime, ref DateTime ChangeTime,
			ref long EndOfFile, ref long AllocationSize, ref long FileId, ref uint FileAttributes)
		{
			Log.WriteLine("[EnumerateDirectory] path={0}, mask={1}, restart={2}", DirectoryInfo.FileName, Mask, Restart);

			if (Restart && EnumerationInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(EnumerationInfo.UserContext);
				hdl.Free();
				EnumerationInfo.UserContext = IntPtr.Zero;
			}

			WdEnumCtx ectx = null;
			if (EnumerationInfo.UserContext == IntPtr.Zero)
			{
				CbfsDisk disk = (CbfsDisk)Sender.Tag;
				ectx = new WdEnumCtx();
				ectx.Items = disk.EnumerateDirectory(DirectoryInfo, HandleInfo, Mask);
				ectx.Index = 0;
				GCHandle hdl = GCHandle.Alloc(ectx);
				EnumerationInfo.UserContext = GCHandle.ToIntPtr(hdl);
			}
			else
			{
				GCHandle hdl = GCHandle.FromIntPtr(EnumerationInfo.UserContext);
				ectx = (WdEnumCtx)hdl.Target;
			}
			Debug.Assert(ectx != null);

			if (ectx.Index < ectx.Items.Count)
			{
				WdFileInfo info = ectx.Items[ectx.Index];
				++ectx.Index;

				FileFound = true;
				FileName = info.Name;
				CreationTime = info.CTime;
				LastAccessTime = info.ATime;
				LastWriteTime = info.MTime;
				EndOfFile = info.FileSize;
				AllocationSize = info.AllocSize;
				FileId = 0;
				FileAttributes = info.Attr;
				Log.WriteLine("item {0}/{1}: {2}", ectx.Index, ectx.Items.Count, info.Name);
			}
			else
			{
				FileFound = false;
				Log.WriteLine("end of list");
			}
		}

		internal static void CbfsCloseDirectoryEnumeration(CallbackFileSystem sender, CbFsFileInfo DirectoryInfo, CbFsDirectoryEnumerationInfo EnumerationInfo)
		{
			Log.WriteLine("[CloseDirectoryEnumeration]");
			if (EnumerationInfo.UserContext != IntPtr.Zero)
			{
				GCHandle hdl = GCHandle.FromIntPtr(EnumerationInfo.UserContext);
				hdl.Free();
				EnumerationInfo.UserContext = IntPtr.Zero;
			}
		}

		internal static void CbfsSetAllocationSize(CallbackFileSystem sender, CbFsFileInfo FileInfo, Int64 AllocationSize)
		{
			Log.WriteLine("[SetAllocationSize]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.SetAllocationSize(FileInfo, AllocationSize);
		}

		internal static void CbfsSetEndOfFile(CallbackFileSystem sender, CbFsFileInfo FileInfo, Int64 EndOfFile)
		{
			Log.WriteLine("[SetEndOfFile]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.SetEndOfFile(FileInfo, EndOfFile);
		}

		internal static void CbfsSetFileAttributes(CallbackFileSystem sender, CbFsFileInfo FileInfo, CbFsHandleInfo HandleInfo,
			DateTime CreationTime, DateTime LastAccessTime, DateTime LastWriteTime, DateTime ChangeTime, uint Attributes)
		{
			Log.WriteLine("[SetFileAttributes]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.SetFileAttributes(FileInfo, HandleInfo, Attributes, CreationTime, LastAccessTime, LastWriteTime);
		}

		internal static void CbfsCanFileBeDeleted(CallbackFileSystem sender, CbFsFileInfo FileInfo, CbFsHandleInfo HandleInfo, ref Boolean CanBeDeleted)
		{
			Log.WriteLine("[CanFileBeDeleted]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			CanBeDeleted = disk.CanFileBeDeleted(FileInfo, HandleInfo);
		}

		internal static void CbfsDeleteFile(CallbackFileSystem sender, CbFsFileInfo FileInfo)
		{
			Log.WriteLine("[DeleteFile]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.DeleteFile(FileInfo);
		}

		internal static void CbfsRenameOrMoveFile(CallbackFileSystem sender, CbFsFileInfo FileInfo, string NewFileName)
		{
			Log.WriteLine("[MoveFile]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			disk.MoveFile(FileInfo, NewFileName);
		}

		internal static void CbfsReadFile(CallbackFileSystem sender, CbFsFileInfo FileInfo, long Position, byte[] Buffer, int BytesToRead, ref int BytesRead)
		{
			Log.WriteLine("[ReadFile]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			BytesRead = disk.ReadFile(FileInfo, Position, Buffer, BytesToRead);
		}

		internal static void CbfsWriteFile(CallbackFileSystem sender, CbFsFileInfo FileInfo, long Position, byte[] Buffer, int BytesToWrite, ref int BytesWritten)
		{
			Log.WriteLine("[WriteFile]");
			CbfsDisk disk = (CbfsDisk)sender.Tag;
			BytesWritten = disk.WriteFile(FileInfo, Position, Buffer, BytesToWrite);
		}

		internal static void CbfsIsDirectoryEmpty(CallbackFileSystem Sender, CbFsFileInfo DirectoryInfo, string FileName, ref Boolean IsEmpty)
		{
			Log.WriteLine("[IsDirectoryEmpty]");
			CbfsDisk disk = (CbfsDisk)Sender.Tag;
			IsEmpty = disk.IsDirectoryEmpty(DirectoryInfo, FileName);
		}
	}

	public class CbfsMounter
	{
		static void InitProxy(CallbackFileSystem cbfs)
		{
			cbfs.OnMount = new CbFsMountEvent(CbfsProxy.CbfsMount);
			cbfs.OnUnmount = new CbFsUnmountEvent(CbfsProxy.CbfsUnmount);
			cbfs.OnGetVolumeSize = new CbFsGetVolumeSizeEvent(CbfsProxy.CbfsGetVolumeSize);
			cbfs.OnGetVolumeLabel = new CbFsGetVolumeLabelEvent(CbfsProxy.CbfsGetVolumeLabel);
			cbfs.OnSetVolumeLabel = new CbFsSetVolumeLabelEvent(CbfsProxy.CbfsSetVolumeLabel);
			cbfs.OnGetVolumeId = new CbFsGetVolumeIdEvent(CbfsProxy.CbfsGetVolumeId);
			cbfs.OnCreateFile = new CbFsCreateFileEvent(CbfsProxy.CbfsCreateFile);
			cbfs.OnOpenFile = new CbFsOpenFileEvent(CbfsProxy.CbfsOpenFile);
			cbfs.OnCloseFile = new CbFsCloseFileEvent(CbfsProxy.CbfsCloseFile);
			cbfs.OnGetFileInfo = new CbFsGetFileInfoEvent(CbfsProxy.CbfsGetFileInfo);
			cbfs.OnEnumerateDirectory = new CbFsEnumerateDirectoryEvent(CbfsProxy.CbfsEnumerateDirectory);
			cbfs.OnCloseDirectoryEnumeration = new CbFsCloseDirectoryEnumerationEvent(CbfsProxy.CbfsCloseDirectoryEnumeration);
			cbfs.OnSetAllocationSize = new CbFsSetAllocationSizeEvent(CbfsProxy.CbfsSetAllocationSize);
			cbfs.OnSetEndOfFile = new CbFsSetEndOfFileEvent(CbfsProxy.CbfsSetEndOfFile);
			cbfs.OnSetFileAttributes = new CbFsSetFileAttributesEvent(CbfsProxy.CbfsSetFileAttributes);
			cbfs.OnCanFileBeDeleted = new CbFsCanFileBeDeletedEvent(CbfsProxy.CbfsCanFileBeDeleted);
			cbfs.OnDeleteFile = new CbFsDeleteFileEvent(CbfsProxy.CbfsDeleteFile);
			cbfs.OnRenameOrMoveFile = new CbFsRenameOrMoveFileEvent(CbfsProxy.CbfsRenameOrMoveFile);
			cbfs.OnReadFile = new CbFsReadFileEvent(CbfsProxy.CbfsReadFile);
			cbfs.OnWriteFile = new CbFsWriteFileEvent(CbfsProxy.CbfsWriteFile);
			cbfs.OnIsDirectoryEmpty = new CbFsIsDirectoryEmptyEvent(CbfsProxy.CbfsIsDirectoryEmpty);
		}

		public static void Mount(CbfsDisk disk)
		{
			const string sRegKey = "289CF30B234C2C087257F489569B988D4AEA109D19AD15054C212EB3B2F2BEA91F1290E508156DF4EE1868A4CCCFC4ACE0B5F65E00DDE496B73B5FC512B426C51E038C15660BA0053029D678020A5FDDE48015746C2F68B2E57B7A1F7CD142A7C4D912D75085CACF88FD96FBDC31FE23C4D922A7E0958E3354E9A64BECC1CE5390C54CED";
			const string guid = "713CC6CE-B3E2-4fd9-838D-E28F558F6866";

			CallbackFileSystem cbfs = disk.Cbfs;
			InitProxy(cbfs);
			cbfs.Tag = disk;

			CallbackFileSystem.SetRegistrationKey(sRegKey);

			// Uncomment  for creation of PnP storage, ejectable from system tray
			// cbfs.StorageType = CbFsStorageType.stDiskPnP;                
			// cbfs.StorageCharacteristics = (int)(CallbackFileSystem.scRemovableMedia + CallbackFileSystem.scShowInEjectionTray + CallbackFileSystem.scAllowEjection);

			CallbackFileSystem.Initialize(guid);
			cbfs.CreateStorage();

			cbfs.MountMedia(0);
			cbfs.AddMountingPoint("P:");
		}

		public static void Unmount(CbfsDisk disk)
		{
			var cbfs = disk.Cbfs;
			cbfs.DeleteMountingPoint(0);
			cbfs.UnmountMedia(true);
			cbfs.DeleteStorage(true);
		}
	}
}
