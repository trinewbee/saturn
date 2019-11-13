using Nano.Xapi.Netdisk.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using Nano.Ext.Error;

namespace Nano.Xapi.Netdisk.Error
{
    public class XE
    {        
        public static void Verify(string stat, string message = null, params object[] args)
        {
            Verify(XError.Succeed(stat), stat, message, args);
        }

        public static void Verify(bool conditions, string stat, string msg = null, params object[] args)
        {
            if (!conditions)
            {
                msg = msg == null ? "" : string.Format(msg, args);
                XError.Throw(stat, msg, null);
            }
        }

        public static void Verify(NdResponse resp, string message = null, params object[] args)
        {
            if (!resp.Succeeded)
            {
                message = message == null ? resp.Message : string.Format(message, args);
                XError.Throw(resp.Stat, message, null);
            }
        }

        public static NdResponse Make(string stat, string format = null, params object[] args)
        {
            Debug.Assert(stat != null);
            var resp = new NdResponse() { Stat = stat };
            resp.Message = format == null ? null : string.Format(format, args);
            return resp;
        }

        public static NdResponse Make(WebException wex, string msg)
        {
            msg = msg ?? wex.Message;
            if (wex.Response == null)
                return new NdResponse() { Message = msg, Stat = XStat.ERROR_UNEXP_NET_ERR };

            try
            {
                using (var resp = wex.Response as HttpWebResponse)
                {
                    if (resp.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        var nr = new NdResponse();
                        nr.ParseResponse(resp);
                        return nr;
                    }
                    return new NdResponse() { Message = wex.Message, Stat = XStat.ERROR_BAD_NET_RESP };
                }
            }
            catch (System.Exception ex)
            {
                return new NdResponse() { Message = ex.Message, Stat = XStat.ERROR_BAD_NET_RESP };
            }
        }
    }
}
