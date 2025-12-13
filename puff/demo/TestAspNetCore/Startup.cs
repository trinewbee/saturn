using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Puff.NetCore;
using TestAspNetCore.Hubs;
using TestAspNetCore.Services;
using Puff.NetCore.SignalR;

using Puff.NetCore.Swagger;
using Microsoft.AspNetCore.SignalR;

namespace TestAspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // 配置Kestrel服务器允许同步I/O操作
            // 这是为了支持同步中间件系统和现有的JmController代码
             services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
                options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L;
            });
           
            var mvcb = services.AddMvc(options =>
            {
                options.OutputFormatters.Insert(0, new JmOutputFormatter());
            });
            mvcb.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSignalR().AddHubOptions<ChatHub>(options => {
                 options.AddFilter<PuffHubFilter>();
            });
            services.AddSingleton<IChatService, ChatService>();

            // Puff Swagger
            services.AddPuffSwagger("TestAspNetCore", "v1");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            //app.UseWebSockets(); // websocket support

            // Enable Puff Swagger
            app.UsePuffSwagger();

            app.UseRouting();
            //app.Use(WebSocketAdapter.WebSocketFilter); // websocket redirection
            //配置 WebSocket 终结点
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/chatHub");
            });
        }


    }
}
