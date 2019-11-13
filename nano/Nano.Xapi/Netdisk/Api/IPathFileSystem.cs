using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Protocal;

namespace Nano.Xapi.Netdisk.Api
{
    public interface IPathFileSystem
    {
        NdFsListResponse ListByPath(string path, int pageIndex = 0, int pageSize = 0, XHttpRequest<NdFsListResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsInfoResponse FileInfoByPath(string path, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse MkdirByPath(string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse RmByPath(IEnumerable<string> paths, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdResponse RenameByPath(string srcPath, string dstPath, bool keep, long pre_fid, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestDownloadResponse RequestDownloadByPath(string path, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestDownloadResponse RequestDownloadByStor(string stor, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdRequestUploadResponse RequestUploadByPath(string path, bool overwrite, bool isCreateDir, long size, long ctime, long mtime, List<NdRequestUploadPartInfo> parts, XHttpRequest<NdRequestUploadResponse>.XCallback callback = null, IBaseInvoker invoker = null);

        NdFsInfoResponse CommitUploadByPath(string path, bool overwrite, bool isCreateDir, long size, string uploadId, long ctime, long mtime, IEnumerable<string> commitIds, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null);
    }
}
