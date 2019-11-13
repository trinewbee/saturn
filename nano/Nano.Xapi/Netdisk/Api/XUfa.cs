using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Error;
using System.Net;
using System.Diagnostics;
using Nano.Xapi.Netdisk;
using System.IO;
using System.Security.Cryptography;
using Nano.Xapi.Netdisk.Model;

namespace Nano.Xapi.Netdisk.Api
{
    public abstract class XUfa : XApiModule
    {
        public const int BLOCK_SIZE = 0x800000;

        public static readonly byte[] EMPTY = new byte[] { 218, 57, 163, 238, 94, 107, 75, 13, 50, 85, 191, 239, 149, 96, 24, 144, 175, 216, 7, 9 };

        public delegate void ProgressHandler(int index, int pos);
        public delegate int ReadHandler(long pos, int length, byte[] buffer, int offset);
        public delegate int PartReadHandler(int index, int pos, int length, byte[] buffer, int offset);
        public delegate int WriteHandler(long pos, int length, byte[] buffer, int offset);
        public delegate int PartWriteHandler(int index, int pos, int length, byte[] buffer, int offset);
        public delegate void SetPartUploader(IUfaPartUploader uploader);
        public delegate void SetPartDownloader(IUfaPartDownloader downloader);

        public interface IUploadPartDelegate
        {
            IUfaPartUploader Uploader { get; set; }

            int OnReadPart(int index, int pos, int length, byte[] buffer, int offset);

            void OnProgress(int index, int pos);
        }

        public class UploadPartDelegate : IUploadPartDelegate
        {
            public ProgressHandler Progress;
            public PartReadHandler ReadPart;

            public IUfaPartUploader Uploader { get; set; }

            public int OnReadPart(int index, int pos, int length, byte[] buffer, int offset)
            {
                return ReadPart.Invoke(index, pos, length, buffer, offset);
            }

            public void OnProgress(int index, int pos)
            {
                Progress?.Invoke(index, pos);
            }
        }

        public interface IDownloadPartDelegate
        {
            IUfaPartDownloader Downloader { get; set; }

            int OnWritePart(int index, int pos, int length, byte[] buffer, int offset);

            void OnProgress(int index, int pos);
        }

        public class DownloadPartDelegate : IDownloadPartDelegate
        {
            public ProgressHandler Progress;
            public PartWriteHandler WritePart;

            public IUfaPartDownloader Downloader { get; set; }

            public int OnWritePart(int index, int pos, int length, byte[] buffer, int offset)
            {
                return WritePart.Invoke(index, pos, length, buffer, offset);
            }

            public void OnProgress(int index, int pos)
            {
                Progress.Invoke(index, pos);
            }
        }

        public static int ReadEmptyPart(int index, int pos, int length, byte[] buffer, int offset)
        {
            return 0;
        }

        public interface IUfaPartUploader
        {
            IUploadPartDelegate Handler { get; }

            NdUploadPartResponse Upload(string addr, string uploadId, int index, int length, int grain, byte[] sha1);

            void Abort();
        }

        public interface IUfaPartDownloader
        {
            IDownloadPartDelegate Handler { get; }

            NdResponse Download(string addr, string partId, int index, int length, int grain, List<Range> ranges);

            void Abort();
        }

        public abstract IUfaPartUploader CreateUploader(IUploadPartDelegate handler, IBaseInvoker invoker = null);

        public abstract IUfaPartDownloader CreateDownloader(IDownloadPartDelegate handler, IBaseInvoker invoker = null);

        public NdUploadPartResponse UploadPart(string addr, string uploadId, int index, int length, int grain, byte[] sha1, PartReadHandler onRead, ProgressHandler onProgress = null, SetPartUploader onSetPartUploader= null)
        {
            var handler = new UploadPartDelegate(){ Progress = onProgress, ReadPart = onRead };
            var udr = handler.Uploader = CreateUploader(handler);
            onSetPartUploader?.Invoke(udr);
            return udr.Upload(addr, uploadId, index, length, grain, sha1);
        }

        public NdUploadPartResponse UploadPart(string addr, string uploadId, int index, int length, int grain, byte[] sha1, IUploadPartDelegate handler)
        {
            var udr = handler.Uploader = CreateUploader(handler);
            return udr.Upload(addr, uploadId, index, length, grain, sha1);
        }

        public NdResponse DownloadPart(string addr, string partId, int index, int length, int grain, List<Range> ranges, PartWriteHandler onWrite, ProgressHandler onProgress = null, SetPartDownloader onSetPartDownloader = null)
        {
            var handler = new DownloadPartDelegate() { Progress = onProgress, WritePart = onWrite };
            var dlr = handler.Downloader = CreateDownloader(handler);
            onSetPartDownloader?.Invoke(dlr);
            return dlr.Download(addr, partId, index, length, grain, ranges);
        }

        public NdResponse DownloadPart(string addr, string partId, int index, int length, int grain, List<Range> ranges, IDownloadPartDelegate handler)
        {
            var dlr = handler.Downloader = CreateDownloader(handler);
            return dlr.Download(addr, partId, index, length, grain, ranges);
        }

        #region utils

        public static List<NdRequestUploadPartInfo> MakeAllParts(long length, long partSize, PartReadHandler onRead)
        {
            var ret = new List<NdRequestUploadPartInfo>();
            if (length == 0)
            {
                ret.Add(new NdRequestUploadPartInfo()
                {
                    Index = 0,
                    Sha1 = EMPTY,
                    Size = 0
                });
                return ret;
            }

            var lb = 0;
            var hb = (length - 1) / partSize;
            var buffer = new byte[partSize];
            var sha1 = SHA1.Create();
            for (var ib = lb; ib <= hb; ib++)
            {
                var ipos = ib * partSize;
                var ilen = (int)(length - ipos > partSize ? partSize : length - ipos);
                var read = onRead(ib, 0, ilen, buffer, 0);
                XE.Verify(read == ilen, XStat.LERR_IO_EXCEPTION, "[XUfa.MakeAllParts] error, file length not matched.");

                byte[] hash = sha1.ComputeHash(buffer, 0, ilen);
                var part = new NdRequestUploadPartInfo() { Size = ilen, Sha1 = hash, Index = ib };
                ret.Add(part);
            }
            return ret;
        }

        public static string CreateUploadURL(string addr, string uploadId, int partIndex, int partSize, byte[] partSha1)
        {
            return addr + "/uploadPart?fileUploadId=" + uploadId + "&partNumber=" + partIndex +
                    "&partSize=" + partSize + "&partSha1=" + UrlSafeBase64(partSha1);
        }

        public static string UrlSafeBase64(byte[] data)
        {
            string bs = Convert.ToBase64String(data);
            bs = bs.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return bs;
        }
        #endregion utils
    }
}
