using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json.Expression;
using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Protocal;

namespace Nano.Xapi.Netdisk.Api
{
    public interface ISvrFileSystem
    {
        NdResponse Lock(long fid, int gid, string lockType, string autoUnlockInterval, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse Unlock(long fid, int gid, string lockType, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsInfoResponse SetAttr(long fid, int gid, int attr, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsListResponse ListByPath(int gid, string path, int pageIndex = 0, int pageSize = 0, XHttpRequest<NdFsListResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsInfoResponse InfoByPath(int gid, string path, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse MkdirByPath(int gid, string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse RmByPath(int gid, string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse RenameByPath(int gid, string src, int dstGid, string dstPath, bool keep, long pre_fid, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse CopyByPath(string srcPath, string dstPath, bool overWrite = true, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse CopyDeleteFile(int srcGid, string srcPath, int dstGid, string dstPath, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestDownloadResponse RequestDownloadByPath(int gid, string path, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestDownloadResponse RequestDownloadByStor(string stor, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestUploadResponse RequestUploadByPath(int gid, string path, bool overwrite, bool isCreateDir, long size, long ctime, long mtime, List<NdRequestUploadPartInfo> parts, XHttpRequest<NdRequestUploadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsInfoResponse CommitUploadByPath(int gid, string path, bool overwrite, bool isCreateDir, long size, string uploadId, long ctime, long mtime, IEnumerable<string> commitIds, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdSyncMTimeResponse GetMTime(int gid, string path, XHttpRequest<NdSyncMTimeResponse>.XCallback callback = null, IBaseInvoker invoker = null, params int[] fids);
    }
}
