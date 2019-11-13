using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json.Expression;
using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Api;

namespace Nano.Xapi.Netdisk.Impls
{
    class XsvrFileSystem : XApiModule, ISvrFileSystem
    {
        public NdResponse Lock(long fid, int gid, string lockType, string autoUnlockInterval, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/lock");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("fid", fid) + JE.Pair("lockType", lockType) + JE.Pair("autoUnlockInterval", autoUnlockInterval) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse Unlock(long fid, int gid, string lockType, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/unlock");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("fid", fid) + JE.Pair("lockType", lockType) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdFsInfoResponse SetAttr(long fid, int gid, int attr, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/setAttr");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("fid", fid) + JE.Pair("attr", attr) + JE.EDict();
            return new XHttpRequest<NdFsInfoResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdFsListResponse ListByPath(int gid, string path, int pageIndex = 0, int pageSize = 0, XHttpRequest<NdFsListResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/listByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.Pair("pageIndex", pageIndex) + JE.Pair("pageSize", pageSize) + JE.EDict();
            return new XHttpRequest<NdFsListResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdFsInfoResponse InfoByPath(int gid, string path, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/infoByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.EDict();
            return new XHttpRequest<NdFsInfoResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse MkdirByPath(int gid, string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/mkdirByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse RmByPath(int gid, string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/rmByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }
        public NdResponse RenameByPath(int gid, string src, int dstGid, string dstPath, bool keep, long pre_fid, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/renameByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("srcPath", src) + JE.Pair("dstGid", dstGid) + JE.Pair("dstPath", dstPath) + JE.Pair("keep", keep) + JE.Pair("pre_fid", pre_fid) + JE.Pair("overWrite", true) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse CopyByPath(string srcPath, string dstPath, bool overWrite = true, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/copyByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("overWrite", true) + JE.Pair("srcPath", srcPath) + JE.Pair("dstPath", dstPath) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }
        public NdResponse CopyDeleteFile(int srcGid, string srcPath, int dstGid, string dstPath, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/copyDeleteFile");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("overWrite", true) + JE.Pair("srcGid", srcGid) + JE.Pair("srcPath", srcPath) + JE.Pair("destGid", dstGid) + JE.Pair("destPath", dstPath) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestDownloadResponse RequestDownloadByPath(int gid, string path, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/requestDownloadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.Pair("pos", pos) + JE.Pair("len", length) + JE.Pair("directUrl", false) + JE.EDict();
            return new XHttpRequest<NdRequestDownloadResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestDownloadResponse RequestDownloadByStor(string stor, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/requestDownloadByStor");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("stor", stor) + JE.Pair("pos", pos) + JE.Pair("len", length) + JE.Pair("directUrl", false) + JE.EDict();
            return new XHttpRequest<NdRequestDownloadResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestUploadResponse RequestUploadByPath(int gid, string path, bool overwrite, bool isCreateDir, long size, long ctime, long mtime, List<NdRequestUploadPartInfo> parts, XHttpRequest<NdRequestUploadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/requestUploadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.Pair("size", size) + JE.Pair("c_ctime", ctime) + JE.Pair("c_mtime", mtime) + JE.Pair("overWrite", overwrite) + JE.Pair("isCreateDir", isCreateDir);
            e = e + JE.List("partsInfo");
            foreach (var part in parts)
            {
                string b64sha1 = XUfa.UrlSafeBase64(part.Sha1);
                e = e + JE.Dict() + JE.Pair("partSize", part.Size) + JE.Pair("partSha1", b64sha1) + JE.EDict();
            }
            e = e + JE.EList();
            e = e + JE.EDict();
            return new XHttpRequest<NdRequestUploadResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdFsInfoResponse CommitUploadByPath(int gid, string path, bool overwrite, bool isCreateDir, long size, string uploadId, long ctime, long mtime, IEnumerable<string> commitIds, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/fs/api/commitUploadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("gid", gid) + JE.Pair("path", path) + JE.Pair("size", size) + JE.Pair("c_ctime", ctime) + JE.Pair("c_mtime", mtime) + JE.Pair("overWrite", overwrite) + JE.Pair("isCreateDir", isCreateDir) +
                JE.Pair("fileUploadId", uploadId);
            e = e + JE.List("partCommitIds");
            foreach (string commitId in commitIds)
            {
                e = e + commitId;
            }
            e = e + JE.EList();
            e = e + JE.EDict();
            return new XHttpRequest<NdFsInfoResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdSyncMTimeResponse GetMTime(int gid, string path, XHttpRequest<NdSyncMTimeResponse>.XCallback callback=null, IBaseInvoker invoker = null, params int[] fids)
        {
            string url = MakeUrl("/sync/api/getMTime");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.List("files");
            foreach (int fid in fids)
                e = e + JE.Dict() + JE.Pair("fid", fid) + JE.Pair("gid", gid) + JE.EDict();
            e = e + JE.EList() + JE.EDict();
            return new XHttpRequest<NdSyncMTimeResponse>(Client, invoker).Send(url, e, callback);
        }

    }
}
