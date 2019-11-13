using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;

namespace Nano.Xapi.Netdisk.Model
{
    public class NdGetUserQuotaResponse : NdResponse
    {
        public long Limited = 0;
        public long Used = 0;
        public long CompanyId = 0; //公司id
        public long FsId;
        public long Time;
        public long Type;
        public long Decrease;
        public long Increase;
        public long Version;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;
            var quota = root["quota"];
            Limited = quota["limited"];
            Used = quota["used"];
            FsId = quota["fsid"];
            CompanyId = quota["cid"];
            Time = quota["time"];
            Type = quota["type"];
            Decrease = quota["decrease"];
            Increase = quota["increase"];
            Version = quota["version"];
        }
    }
}
