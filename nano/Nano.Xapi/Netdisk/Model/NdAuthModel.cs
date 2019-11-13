using Nano.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nano.Xapi.Netdisk.Model
{
    public class NdLoginResponse : NdResponse
    {
        public string Token = null;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;
            Token = root["token"];
        }
    }
}
