using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CallbackFS;

namespace Nano.Cbfs.Test
{
    class FileItem
    {
        public string Name;
        public FileItem Parent;
        public uint Attr;
        public long Size, Alloc;
        public List<FileItem> Children;
        public DateTime CTime, MTime, ATime;   // all in UTC
        public MemoryStream Data;

        public void Init(string name, uint attr, DateTime ctime, DateTime mtime, DateTime atime)
        {
            this.Name = name;
            this.Parent = null;
            this.Attr = attr;
            this.Size = this.Alloc = 0;
            this.CTime = ctime;
            this.MTime = mtime;
            this.ATime = atime;
            if (IsDir)
                this.Children = new List<FileItem>();
            else
                this.Data = new MemoryStream();
        }

        public bool IsDir
        {
            get { return (Attr & (WdFileInfo.FILE_ATTRIBUTE_DEVICE | WdFileInfo.FILE_ATTRIBUTE_DIRECTORY)) != 0; }
        }

        public bool IsEmpty
        {
            get { return Children == null || Children.Count == 0; }
        }

        public FileItem GetChild(string name)
        {
            name = name.ToLowerInvariant();
            if (!IsDir)
                return null;

            foreach (var item in Children)
            {
                if (item.Name.ToLowerInvariant() == name)
                    return item;
            }
            return null;
        }

        public void AddChild(FileItem item)
        {
            Debug.Assert(IsDir && item.Parent == null);
            Children.Add(item);
            item.Parent = this;
        }

        public void RemoveChild(FileItem item)
        {
            Debug.Assert(IsDir && item.Parent == this);
            if (!Children.Remove(item))
                throw new ECBFSError(ErrCode.ERROR_FILE_NOT_FOUND);
            item.Parent = null;
        }

        public WdFileInfo ToFileInfo()
        {
            var info = new WdFileInfo();
            info.Name = this.Name;
            info.Attr = this.Attr;
            info.CTime = this.CTime;
            info.MTime = this.MTime;
            info.ATime = this.ATime;
            info.FileSize = this.Size;
            info.AllocSize = this.Alloc;
            return info;
        }

        public static FileItem FindFile(FileItem item, string path)
        {
            Debug.Assert(path[0] == '\\');
            int pos = 1, len = path.Length;
            while (pos < len)
            {
                int pos2 = path.IndexOf('\\', pos);
                if (pos2 < 0)
                    pos2 = len;
                string name = path.Substring(pos, pos2 - pos);
                item = item.GetChild(name);
                if (item == null)
                    return null;
                pos = pos2 < len ? pos2 + 1 : len;
            }
            return item;
        }

        public static FileItem FindParentDirectory(FileItem item, string path, out string name)
        {
            Debug.Assert(path[0] == '\\' && path[path.Length - 1] != '\\');
            int pos = path.LastIndexOf('\\');
            Debug.Assert(pos >= 2 || pos == 0);
            name = path.Substring(pos + 1);
            if (pos > 0)
                return FindFile(item, path.Substring(0, pos));
            else
                return item;
        }
    }

    public class MinCbfsDisk : CbfsDisk
    {
        CallbackFileSystem m_cbfs;
        long m_totalSectors, m_usedSectors;
        string m_label;
        FileItem m_root;

        public MinCbfsDisk()
        {
            m_cbfs = new CallbackFileSystem();

            m_totalSectors = 0x100000L; // 1MB sectors = 512MB
            m_usedSectors = 0;
            m_label = "Test Drive";

            DateTime ft = DateTime.UtcNow;
            m_root = new FileItem();
            m_root.Init(null, WdFileInfo.FILE_ATTRIBUTE_DEVICE | WdFileInfo.FILE_ATTRIBUTE_DIRECTORY, ft, ft, ft);
        }

        public CallbackFileSystem Cbfs
        {
            get { return m_cbfs; }
        }

        public void Mount() { }
        public void Unmount() { }

        public void GetVolumeSize(out long pTotalNumberOfSectors, out long pNumberOfFreeSectors)
        {
            pTotalNumberOfSectors = m_totalSectors;
            pNumberOfFreeSectors = m_totalSectors - m_usedSectors;
        }

        public string GetVolumeLabel() { return m_label; }
        public void SetVolumeLabel(string label) { m_label = label; }
        public uint GetVolumeID() { return 0x12345678; }

        public void CreateFile(string path, uint desiredAccess, uint attrs, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
        {
            string name;
            FileItem parent = FileItem.FindParentDirectory(m_root, path, out name);
            if (parent == null)
                throw new ECBFSError(ErrCode.ERROR_INVALID_PARAMETER);
            if (parent.GetChild(name) != null)
                throw new ECBFSError(ErrCode.ERROR_ALREADY_EXISTS);

            DateTime ft = DateTime.UtcNow;
            FileItem item = new FileItem();
            item.Init(name, attrs, ft, ft, ft);
            parent.AddChild(item);

            Debug.Assert(fileInfo.UserContext == IntPtr.Zero);
            GCHandle hdl = GCHandle.Alloc(item);
            fileInfo.UserContext = GCHandle.ToIntPtr(hdl);
        }

        public void OpenFile(string path, uint desiredAccess, uint fileAttributes, uint shareMode, CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
        {
            if (fileInfo.UserContext != IntPtr.Zero)
                return;

            FileItem item = FileItem.FindFile(m_root, path);
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
            FileItem item = FileItem.FindFile(m_root, path);
            if (item == null)
                return null;

            return item.ToFileInfo();
        }

        public List<WdFileInfo> EnumerateDirectory(CbFsFileInfo directoryInfo, CbFsHandleInfo handleInfo, string mask)
        {
            string path = directoryInfo.FileName;
            FileItem parent = FileItem.FindFile(m_root, path);
            if (parent == null)
                throw new ECBFSError(ErrCode.ERROR_PATH_NOT_FOUND);

            List<WdFileInfo> infos = new List<WdFileInfo>();
            WildcardMatch wm = new WildcardMatch();
            wm.Init(mask);

            foreach (var subitem in parent.Children)
            {
                if (wm.Match(subitem.Name))
                {
                    WdFileInfo info = subitem.ToFileInfo();
                    infos.Add(info);
                }
            }

            return infos;
        }

        public void SetAllocationSize(CbFsFileInfo fileInfo, long allocSize)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);

            if (allocSize < item.Size)    // not sure
                return;

            Debug.Assert(allocSize < 0x20000000);
            int cluster = (int)m_cbfs.ClusterSize;
            this.m_usedSectors -= (item.Alloc + cluster - 1) / cluster;
            item.Alloc = allocSize;
            item.Data.Capacity = (int)item.Alloc;
            this.m_usedSectors += (item.Alloc + cluster - 1) / cluster;
        }

        public void SetEndOfFile(CbFsFileInfo fileInfo, long size)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);
            Debug.Assert(item.Alloc >= size);
            item.Size = size;
            item.Data.SetLength(size);
        }

        public void SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo, uint attrs,
            DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);
            if (attrs != 0)
                item.Attr = attrs;
            if (creationTime != DateTime.MinValue)
                item.CTime = creationTime;
            if (lastAccessTime != DateTime.MinValue)
                item.ATime = lastAccessTime;
            if (lastWriteTime != DateTime.MinValue)
                item.MTime = lastWriteTime;
        }

        public bool CanFileBeDeleted(CbFsFileInfo fileInfo, CbFsHandleInfo handleInfo)
        {
            return true;
        }

        public void DeleteFile(CbFsFileInfo fileInfo)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);

            int cluster = (int)m_cbfs.ClusterSize;
            this.m_usedSectors -= (item.Alloc + cluster - 1) / cluster;

            FileItem parent = item.Parent;
            Debug.Assert(parent != null);
            parent.RemoveChild(item);

            if (fileInfo.UserContext != IntPtr.Zero)
            {
                GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
                hdl.Free();
                fileInfo.UserContext = IntPtr.Zero;
            }
        }

        public void MoveFile(CbFsFileInfo fileInfo, string NewFileName)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);

            string name;
            FileItem parentNew = FileItem.FindParentDirectory(m_root, NewFileName, out name);
            if (parentNew == null)
                throw new ECBFSError(ErrCode.ERROR_INVALID_PARAMETER);

            if (parentNew != item.Parent)
            {
                item.Parent.RemoveChild(item);
                parentNew.AddChild(item);
            }
            else
            {
                Debug.Assert(parentNew.GetChild(name) == null);
                item.Name = name;
            }
        }

        public int ReadFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);
            item.Data.Seek(pos, SeekOrigin.Begin);
            return item.Data.Read(data, 0, len);
        }

        public int WriteFile(CbFsFileInfo fileInfo, long pos, byte[] data, int len)
        {
            FileItem item = _GetItemFromFileInfo(fileInfo);
            long newSize = pos + len;
            if (newSize > item.Size)
            {
                if (newSize > item.Alloc)
                    this.SetAllocationSize(fileInfo, newSize);
                this.SetEndOfFile(fileInfo, newSize);
            }

            Debug.Assert(pos + len <= item.Size);
            item.Data.Seek(pos, SeekOrigin.Begin);
            item.Data.Write(data, 0, len);
            item.MTime = DateTime.UtcNow;
            return len;
        }

        public bool IsDirectoryEmpty(CbFsFileInfo directoryInfo, string path)
        {
            FileItem item = _GetItemFromFileInfo(directoryInfo);
            return item.IsEmpty;
        }

        FileItem _GetItemFromFileInfo(CbFsFileInfo fileInfo)
        {
            if (fileInfo.UserContext != IntPtr.Zero)
            {
                GCHandle hdl = GCHandle.FromIntPtr(fileInfo.UserContext);
                return (FileItem)hdl.Target;
            }

            string path = fileInfo.FileName;
            return FileItem.FindFile(m_root, path);
        }
    }
}
