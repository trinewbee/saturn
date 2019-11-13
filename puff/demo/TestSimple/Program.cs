using System;
using System.Net;
using Nano.Json;
using Nano.Nuts;
using Puff.Model;
using Puff.Servers;

namespace TestSimple
{
    [IceService(BaseUrl = "/api")]
    class TestApi
    {
        [IceApi()]
        public void Ping() { }

        [IceApi(Ret = "m")]
        public string Hello(string name, int age) => $"My name is {name}. I'm {age} yrs old.";

        [IceApi(Ret = "name,age")]
        public Tuple<string, int> Info(string name, int age) => new Tuple<string, int>(name, age);

        [IceApi()]
        public (string name, int age) InfoV(string name, int age) => (name, age);

        [IceApi(Ret = "info")]
        public object InfoCG(string name, int age) => new { name, age };

        [IceApi(Ret = "info")]
        public DObject InfoDO(string name, int age) => DObject.New(new { name = name, age = age });

        #region Json Style - Stat

        [IceApi()]
        public void Throw(string s) => throw new NutsException(s);

        [IceApi()]
        public JsonNode Stat() => new DObject.DMap { { "stat", "false" }, { "value", 100 } }.ToJson();

        [IceApi(Ret = "value", Stat = "")]
        public int NoStat() => 100;

        #endregion

        [IceApi(Flags = IceApiFlag.Http)]
        public void Raw(string url, HttpListenerContext ctx)
        {
            Nano.Ext.Web.ResponseWriter.SendResponse(ctx.Response, "Haruhi");
        }

        // 内置的 QueryString 集合对中文解析会乱码
        System.Collections.Specialized.NameValueCollection ParseQueryString(HttpListenerRequest request)
        {
            var url = System.Web.HttpUtility.UrlDecode(request.RawUrl);
            var query = url.Substring(url.IndexOf('?') + 1);
            var qs = System.Web.HttpUtility.ParseQueryString(query);
            return qs;
        }
    }

    class Program
    {
        static void StartServer()
        {
            uint portWeb = 8080, portWebSock = 8081;
            var smb = new PuffServer();
            try
            {
                smb.AddService(new TestApi());
                smb.StartServer(portWeb: portWeb, portWebSock: portWebSock);
                Console.WriteLine("Press enter key");
                Console.ReadLine();
                smb.StopServer();
            }
            catch (System.Net.HttpListenerException e)
            {
                Console.WriteLine("Failed to start HTTP server: " + e.Message);
                Console.WriteLine("Try run the following command as administrator, then restart this application");
                Console.WriteLine($"netsh http add urlacl url=http://+:{portWeb}/ user=Everyone");
            }
        }

        static void Main(string[] args)
        {
            Puff.Server.UnitTest.TestProject.Run();
            StartServer();
        }
    }
}
