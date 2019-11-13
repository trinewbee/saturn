using Nano.Xapi.Netdisk.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XHttpResponse : XBaseResponse
    {
        public override void Parse()
        {
            var httpr = Request.Dettach() as HttpWebRequest;
            XE.Verify(httpr != null, Request.IsAborted ? XStat.ERROR_CONNECTION_ABORTED : XStat.ERROR_INTERNAL_ERROR);

            using (var response = httpr.GetResponse())
            {
                ParseResponse(response as HttpWebResponse);
            }
        }
        public virtual void ParseResponse(HttpWebResponse r)
        {

        }
    }
}
