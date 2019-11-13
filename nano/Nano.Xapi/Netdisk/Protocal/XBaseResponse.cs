using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Protocal
{
    public class XBaseResponse
    {
        public string Stat;
        public string Message;

        public XBaseRequest Request { get; protected set; }

        public void Init(XBaseRequest req)
        {
            Request = req;
        }

        public virtual void Error(string stat, string message = null)
        {
            Stat = stat; Message = message;
        }

        public virtual void Parse()
        {
            
        }        
    }
}
