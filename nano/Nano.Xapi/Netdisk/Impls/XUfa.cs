using System;
using System.Collections.Generic;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Error;
using System.Diagnostics;
using System.IO;
using Nano.Xapi.Netdisk.Model;
using Nano.Xapi.Netdisk.Api;
using Nano.Ext.Logs;

namespace Nano.Xapi.Netdisk.Impls
{
    class XsvrUfa : XUfa
    {

        public class XUfaPartDownloader : XHttpRequest<NdResponse>, IBaseMethod, IUfaPartDownloader
        {
            public int Grain;
            public int Length;
            public int Index;
            public IDownloadPartDelegate Handler { get; }

            public XUfaPartDownloader(XBaseClient client, IBaseInvoker invoker, IDownloadPartDelegate handler) : base(client, invoker)
            {
                Handler = handler;
            }

            public NdResponse Download(string addr, string partId, int index, int length, int grain, List<Range> ranges)
            {
                var url = addr + "/downloadPart?partDownloadId=" + partId;
                Index = index;
                Length = length;
                Grain = grain;
                Ranges = ranges;
                return Send(url, this, null);
            }

            public void Invoke(XBaseRequest req)
            {
                var c = Client as XHttpClient;
                var httpr = c.CreateRequest(req.Url, "GET", 10 * 60 * 1000, true);
                httpr.ReadWriteTimeout = 10 * 60 * 1000;
                req.Attach(httpr);
                c.InitHeaders(req);
                c.BindProxy(req);

                var buffer = new byte[Grain];
                using (var resp = httpr.GetResponse())
                using (var rstream = resp.GetResponseStream())
                using (var stream = new MemoryStream(Length))
                {
                    rstream.CopyTo(stream);
                    var count = Ranges.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var r = Ranges[i];
                        var from = (int)r.From;
                        var length = (int)(r.To - r.From);

                        XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
                        stream.Seek(0, SeekOrigin.Begin);
                        var read = stream.Read(buffer, 0, length);
                        Debug.Assert(read == length);

                        XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
                        Handler.OnWritePart(Index, from, length, buffer, 0);
                        Handler.OnProgress(Index, from + length);
                    }
                }
                req.Error(XStat.OK);
                httpr = null;
            }
        }

        public class XUfaPartUploader : XHttpRequest<NdUploadPartResponse>, IUfaPartUploader, IBaseMethod
        {
            public int Grain;
            public int Index;
            public int Length;
            public IUploadPartDelegate Handler { get; }

            public XUfaPartUploader(XBaseClient client, IBaseInvoker invoker, IUploadPartDelegate handler) : base(client, invoker)
            {
                Handler = handler;
            }

            public NdUploadPartResponse Upload(string addr, string uploadId, int index, int length, int grain, byte[] sha1)
            {
                var url = CreateUploadURL(addr, uploadId, index, length, sha1);
                Index = index;
                Length = length;
                Grain = grain;
                return Send(url, this, null);
            }

            public void Invoke(XBaseRequest req)
            {
                var c = Client as XHttpClient;
                var httpr = c.CreateRequest(Url, "POST", 10 * 60 * 1000, true);
                httpr.ReadWriteTimeout = 10 * 60 * 1000;
                req.Attach(httpr);
                c.InitHeaders(req);
                c.BindProxy(req);

                var left = Length;
                var buffer = new byte[Grain];
                var pos = 0;

                var timer = new Stopwatch(); timer.Start();
                Log.ad("UploadPart", "Start {0} : {1} : start", req.Url, Index);

                using (var stream = httpr.GetRequestStream())
                {
                    while (true)
                    {
                        var size = left > Grain ? Grain : left;
                        var read = Handler.OnReadPart(Index, pos, size, buffer, 0);
                        XE.Verify(read == size, XStat.LERR_IO_EXCEPTION, "[UfaPartUploader]: read size not matched");

                        XE.Verify(!req.IsAborted, XStat.ERROR_CONNECTION_ABORTED);
                        stream.Write(buffer, 0, read);

                        left -= read;
                        pos += read;

                        Handler.OnProgress(Index, pos);

                        if (left <= 0)
                        {
                            break;
                        }
                    }
                }

                timer.Stop();Log.ad("UploadPart", "Sent {0}: {1} - {2}", req.Url, Index, timer.ElapsedMilliseconds);timer.Restart();

                c.FetchData(req);

                Log.ad("UploadPart", "Fetched {0}:{1} -{2}", req.Url, Index, timer.ElapsedMilliseconds);

                httpr = null;
            }
        }

        public override IUfaPartDownloader CreateDownloader(IDownloadPartDelegate handler, IBaseInvoker invoker)
        {
            return new XUfaPartDownloader(Client, invoker, handler);
        }

        public override IUfaPartUploader CreateUploader(IUploadPartDelegate handler, IBaseInvoker invoker)
        {
            return new XUfaPartUploader(Client, invoker, handler);
        }
    }
}
