using Nano.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nano;

namespace Nano.Xapi.Netdisk
{
    public class ServerConfig
    {
        public string Host;
        public uint Port;
        public string Protocal = "Http";
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
    }

    public class ProxyConfig
    {
        public string Host;
        public uint Port;
        public string User;
        public string Password;
        public int Type;
    }
}
