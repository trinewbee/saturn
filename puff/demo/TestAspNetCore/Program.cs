using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nano.Logs;
using Puff.NetCore;

namespace TestAspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            FilterLog.Init(new string[] { "wps_sid", "x", "name" });
            Logger.Init("./logs", null, false, 69206016);
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.ClearProviders())
                .UseUrls("http://0.0.0.0:9000")
                .UseStartup<Startup>();
    }
}
