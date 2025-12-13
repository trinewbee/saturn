using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Nano.Logs;
using Nano.Nuts;

namespace Puff.NetCore.SignalR
{
    public class PuffHubFilter : IHubFilter
    {
        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            // 1. 获取 HttpContext
            var httpContext = invocationContext.Context.GetHttpContext();
            if (httpContext == null)
            {
                // 非 HTTP 传输时的降级处理
                return await next(invocationContext);
            }

            // 2. 初始化 Puff Env
            // Env 构造函数会自动赋值给 WebGlobal.curEnv (现在是 AsyncLocal)
            var env = new Env(httpContext.Request);
            
            // 补充 SignalR 特有日志参数
            env.AddLogParam("Transport", new Nano.Json.JsonNode( "SignalR"));
            env.AddLogParam("HubMethod", new Nano.Json.JsonNode(invocationContext.HubMethodName));
            env.AddLogParam("ConnectionId", new Nano.Json.JsonNode(invocationContext.Context.ConnectionId));

            try
            {
                // 3. 执行 Hub 方法
                var result = await next(invocationContext);
                return result;
            }
            catch (Exception ex)
            {
                // 4. 统一异常日志记录
                Logger.Err(env.reqId + "\tSignalR Error: " + ex.Message, ex.StackTrace);
                throw;
            }
            finally
            {
                // 5. 访问日志 (可选 - 这里简单打印一下，或者复用 JmController 中的 _AccessLog 逻辑，但 _AccessLog 是私有的)
                // 这里暂时不调用 _AccessLog，以免涉及过多改动
            }
        }
    }
}
