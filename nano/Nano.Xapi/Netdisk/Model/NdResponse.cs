using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Xapi.Netdisk.Protocal;
using Nano.Xapi.Netdisk.Error;
using System.Net;
using Nano.Net;
using Nano.Json;
using Nano.Ext.Error;

namespace Nano.Xapi.Netdisk.Model
{
    public class NdResponse : XHttpResponse
    {
        public virtual bool Succeeded
        {
            get { return Stat == "OK"; }
        }

        public override void ParseResponse(HttpWebResponse r)
        {
            var resp = ResponseReader.ReadResponseText(r);
            if (Request != null)
            {
                Request.Result = resp;
            }
            try
            {
                JsonNode node = JsonParser.ParseText(resp);
                Stat = node["stat"].TextValue;

                var v = node.GetChildItem("errText");
                if (v != null)
                    Message = v.TextValue;
                ParseJson(node);
            }
            catch (Exception ex)
            {
                throw new XError(XStat.ERROR_BAD_NET_RESP, ex.Message, ex);
            }
        }

        protected virtual void ParseJson(JsonNode root)
        {

        }
    }
}
