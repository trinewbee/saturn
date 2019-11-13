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
    class XPathFileSystem : XApiModule, IPathFileSystem
    {
        public NdFsListResponse ListByPath(string path, int pageIndex = 0, int pageSize = 0, XHttpRequest<NdFsListResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/listByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.Pair("pageIndex", pageIndex) + JE.Pair("pageSize", pageSize) + JE.EDict();
            return new XHttpRequest<NdFsListResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdFsInfoResponse FileInfoByPath(string path, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/fileInfoByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.EDict();
            return new XHttpRequest<NdFsInfoResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse MkdirByPath(string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/mkdirByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdResponse RmByPath(string path, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            var list = new List<string>() { path };
            return RmByPath(list);
        }

        public NdResponse RmByPath(IEnumerable<string> paths, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/rmByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token);
            e = e + JE.List("paths");
            foreach(var p in paths)
                e = e + p;
            e = e + JE.EList();
            e = e + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }
        public NdResponse RenameByPath(string srcPath, string dstPath, bool keep, long pre_fid, XHttpRequest<NdResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/renameByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("srcPath", srcPath) + JE.Pair("dstPath", dstPath) + JE.Pair("keep", keep) + JE.Pair("pre_fid", pre_fid) + JE.Pair("overWrite", true) + JE.EDict();
            return new XHttpRequest<NdResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestDownloadResponse RequestDownloadByPath(string path, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/requestDownloadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.Pair("pos", pos) + JE.Pair("len", length) + JE.Pair("directUrl", false) + JE.EDict();
            return new XHttpRequest<NdRequestDownloadResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestDownloadResponse RequestDownloadByStor(string stor, long pos, long length, XHttpRequest<NdRequestDownloadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/requestDownloadByStor");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("stor", stor) + JE.Pair("pos", pos) + JE.Pair("len", length) + JE.Pair("directUrl", false) + JE.EDict();
            return new XHttpRequest<NdRequestDownloadResponse>(Client, invoker).Send(url, e, callback);
        }

        public NdRequestUploadResponse RequestUploadByPath(string path, bool overwrite, bool isCreateDir, long size, long ctime, long mtime, List<NdRequestUploadPartInfo> parts, XHttpRequest<NdRequestUploadResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/requestUploadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.Pair("size", size) + JE.Pair("c_ctime", ctime) + JE.Pair("c_mtime", mtime) + JE.Pair("overWrite", overwrite) + JE.Pair("isCreateDir", isCreateDir);
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

        public NdFsInfoResponse CommitUploadByPath(string path, bool overwrite, bool isCreateDir, long size, string uploadId, long ctime, long mtime, IEnumerable<string> commitIds, XHttpRequest<NdFsInfoResponse>.XCallback callback = null, IBaseInvoker invoker = null)
        {
            string url = MakeUrl("/file/api/commitUploadByPath");
            JE e = JE.New() + JE.Dict() + JE.Pair("token", Ctx.Token) + JE.Pair("path", path) + JE.Pair("size", size) + JE.Pair("c_ctime", ctime) + JE.Pair("c_mtime", mtime) + JE.Pair("overWrite", overwrite) + JE.Pair("isCreateDir", isCreateDir) +
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

    }
}
