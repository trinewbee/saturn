using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Puff.NetCore;

namespace TestAspNetCore
{
    #region 业务中间件特性

    /// <summary>
    /// 要求身份验证的特性标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireAuthAttribute : BaseMiddlewareAttribute
    {
        /// <summary>
        /// 所需的角色列表
        /// </summary>
        public string[] Roles { get; set; }
        
        /// <summary>
        /// 是否允许匿名访问
        /// </summary>
        public bool AllowAnonymous { get; set; }
        
        /// <summary>
        /// 创建认证中间件实例
        /// </summary>
        public override IPuffMiddleware CreateMiddleware()
        {
            return new TokenValidationMiddleware();
        }
        
        /// <summary>
        /// 认证中间件优先级较高
        /// </summary>
        public override int Priority => 10;
    }

    /// <summary>
    /// 限流配置特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : BaseMiddlewareAttribute
    {
        /// <summary>
        /// 每分钟允许的请求次数
        /// </summary>
        public int RequestsPerMinute { get; }
        
        public RateLimitAttribute(int requestsPerMinute)
        {
            RequestsPerMinute = requestsPerMinute;
        }
        
        /// <summary>
        /// 创建限流中间件实例
        /// </summary>
        public override IPuffMiddleware CreateMiddleware()
        {
            return new RateLimitMiddleware();
        }
        
        /// <summary>
        /// 限流中间件优先级中等
        /// </summary>
        public override int Priority => 50;
    }

    /// <summary>
    /// 审计配置特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuditAttribute : BaseMiddlewareAttribute
    {
        /// <summary>
        /// 操作描述
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// 是否记录请求参数
        /// </summary>
        public bool LogParameters { get; set; } = true;
        
        /// <summary>
        /// 是否记录响应结果
        /// </summary>
        public bool LogResponse { get; set; } = false;
        
        /// <summary>
        /// 创建审计中间件实例
        /// </summary>
        public override IPuffMiddleware CreateMiddleware()
        {
            return new AuditMiddleware();
        }
        
        /// <summary>
        /// 审计中间件优先级较低
        /// </summary>
        public override int Priority => 90;
    }

    #endregion

    #region 业务中间件实现

    /// <summary>
    /// 业务示例：请求日志中间件
    /// </summary>
    public class RequestLoggingMiddleware : IPuffMiddleware
    {
        public bool Invoke(IPuffContext context, Action next)
        {
            try
            {
                next();
                return true;
            }
            catch (Exception ex)
            {
                // 记录请求处理异常
                throw;
            }
        }
    }

    /// <summary>
    /// 业务示例：Token验证中间件
    /// </summary>
    public class TokenValidationMiddleware : IPuffMiddleware
    {
        public bool Invoke(IPuffContext context, Action next)
        {
            // 从Header中提取token
            var token = ExtractToken(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                context.Response = IceApiResponse.Error("Unauthorized", "Token is required");
                context.IsAborted = true;
                return false;
            }
            
            // 简单的token验证（实际项目中应该更复杂）
            if (token != "valid-token-123")
            {
                context.Response = IceApiResponse.Error("Unauthorized", "Invalid token");
                context.IsAborted = true;
                return false;
            }
            
            // 将用户信息存储在上下文中
            context.Items["UserId"] = 123;
            context.Items["Token"] = token;
            
            next();
            return true;
        }
        
        private string ExtractToken(IceApiRequest request)
        {
            if (request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return authHeader?.Replace("Bearer ", "");
            }
            
            if (request.Query.TryGetValue("token", out var tokenQuery))
            {
                return tokenQuery;
            }
            
            return null;
        }
    }

    /// <summary>
    /// 业务示例：限流中间件
    /// </summary>
    public class RateLimitMiddleware : IPuffMiddleware
    {
        private static readonly Dictionary<string, List<DateTime>> _requestTimes = new Dictionary<string, List<DateTime>>();
        private static readonly object _lock = new object();
        
        public bool Invoke(IPuffContext context, Action next)
        {
            // 获取限流配置
            var limitAttr = context.MethodInfo.GetAttribute<RateLimitAttribute>();
            var requestsPerMinute = limitAttr?.RequestsPerMinute ?? 10; // 默认每分钟10次
            
            // 构建限流key（IP + 方法名）
            var clientIp = GetClientIp(context.Request);
            var limitKey = $"{clientIp}:{context.ControllerName}.{context.ActionName}";
            
            lock (_lock)
            {
                var now = DateTime.Now;
                var oneMinuteAgo = now.AddMinutes(-1);
                
                if (!_requestTimes.TryGetValue(limitKey, out var times))
                {
                    times = new List<DateTime>();
                    _requestTimes[limitKey] = times;
                }
                
                // 清理超过1分钟的记录
                times.RemoveAll(t => t < oneMinuteAgo);
                
                // 检查是否超过限制
                if (times.Count >= requestsPerMinute)
                {
                    context.Response = IceApiResponse.Error("RateLimitExceeded", $"Too many requests. Limit: {requestsPerMinute} per minute");
                    context.IsAborted = true;
                    return false;
                }
                
                // 记录当前请求时间
                times.Add(now);
            }
            
            next();
            return true;
        }
        
        private string GetClientIp(IceApiRequest request)
        {
            // 尝试从各种Header中获取真实IP
            if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
            
            if (request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                return realIp;
            }
            
            return "unknown";
        }
    }

    /// <summary>
    /// 业务示例：审计日志中间件
    /// </summary>
    public class AuditMiddleware : IPuffMiddleware
    {
        public bool Invoke(IPuffContext context, Action next)
        {
            // 获取审计配置
            var auditAttr = context.MethodInfo.GetAttribute<AuditAttribute>();
            
            var auditInfo = new
            {
                UserId = context.Items.ContainsKey("UserId") ? context.Items["UserId"] : null,
                Action = auditAttr?.Action ?? $"{context.ControllerName}.{context.ActionName}",
                RequestId = context.RequestId,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIp(context.Request),
                UserAgent = context.Request.Headers.ContainsKey("User-Agent") ? context.Request.Headers["User-Agent"] : "unknown"
            };
            
            // 记录审计日志
            Console.WriteLine($"[AUDIT] {auditInfo.Timestamp:yyyy-MM-dd HH:mm:ss} | {auditInfo.Action} | User: {auditInfo.UserId} | IP: {auditInfo.IpAddress} | RequestId: {auditInfo.RequestId}");
            
            try
            {
                next();
                Console.WriteLine($"[AUDIT] {auditInfo.Action} completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT] {auditInfo.Action} failed: {ex.Message}");
                throw;
            }
        }
        
        private string GetClientIp(IceApiRequest request)
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
            
            if (request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                return realIp;
            }
            
            return "unknown";
        }
    }

    /// <summary>
    /// 业务示例：简单性能监控中间件
    /// </summary>
    public class PerformanceMonitoringMiddleware : IPuffMiddleware
    {
        public bool Invoke(IPuffContext context, Action next)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                next();
                stopwatch.Stop();
                
                // 记录性能数据
                Console.WriteLine($"[PERF] {context.ControllerName}.{context.ActionName} 执行时间: {stopwatch.ElapsedMilliseconds}ms");
                
                // 如果执行时间超过阈值，记录警告
                if (stopwatch.ElapsedMilliseconds > 1000) // 1秒阈值
                {
                    Console.WriteLine($"[PERF-WARNING] 慢查询检测: {context.ControllerName}.{context.ActionName} 耗时 {stopwatch.ElapsedMilliseconds}ms");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[PERF] {context.ControllerName}.{context.ActionName} 执行失败，耗时: {stopwatch.ElapsedMilliseconds}ms, 错误: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 业务示例：缓存中间件
    /// </summary>
    public class SimpleCacheMiddleware : IPuffMiddleware
    {
        private static readonly Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();
        private static readonly object _cacheLock = new object();
        
        public bool Invoke(IPuffContext context, Action next)
        {
            // 简单的GET请求缓存
            if (context.Request.Method != "GET")
            {
                next();
                return true;
            }
            
            var cacheKey = $"{context.ControllerName}.{context.ActionName}";
            
            lock (_cacheLock)
            {
                // 检查缓存
                if (_cache.TryGetValue(cacheKey, out var cachedItem))
                {
                    if (DateTime.UtcNow < cachedItem.ExpiryTime)
                    {
                        context.Response = cachedItem.Response;
                        return true;
                    }
                    else
                    {
                        _cache.Remove(cacheKey);
                    }
                }
            }
            
            // 缓存未命中，执行业务逻辑
            next();
            
            // 缓存响应（简单示例，1分钟过期）
            if (context.Response != null)
            {
                lock (_cacheLock)
                {
                    _cache[cacheKey] = new CacheItem
                    {
                        Response = context.Response,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(1)
                    };
                }
            }
            
            return true;
        }
        
        private class CacheItem
        {
            public IceApiResponse Response { get; set; }
            public DateTime ExpiryTime { get; set; }
        }
    }

    #endregion
}
