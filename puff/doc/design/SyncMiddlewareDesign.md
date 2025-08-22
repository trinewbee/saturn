# Puff框架特性驱动同步中间件系统设计

## 概述

本文档描述了Puff Web框架的**特性驱动同步中间件系统**，该系统基于方法特性自动发现和执行中间件，实现零配置、类型安全的中间件管道。系统完全兼容现有的 `_MethodDispatch` 同步架构，提供简洁易用的拦截机制。

## 1. 核心设计理念

### 1.1 特性驱动架构
- **零配置**：无需复杂的启动配置，通过特性声明中间件
- **编译时确定**：中间件配置在编译时确定，类型安全
- **声明式**：直接在方法或类上声明所需的中间件
- **优先级控制**：通过Priority属性精确控制执行顺序

### 1.2 核心原则
1. **简单胜过复杂**：优先考虑易用性和可读性
2. **类型安全**：编译时检查，避免运行时错误
3. **性能优先**：最小化运行时开销
4. **渐进式采用**：不影响现有代码，可逐步添加中间件

## 2. 核心接口设计

### 2.1 中间件上下文接口

```csharp
/// <summary>
/// 同步中间件执行上下文
/// </summary>
public interface IPuffContext
{
    /// <summary>
    /// 当前请求的唯一标识符
    /// </summary>
    string RequestId { get; }
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    string ControllerName { get; }
    
    /// <summary>
    /// 动作方法名称
    /// </summary>
    string ActionName { get; }
    
    /// <summary>
    /// 原始HTTP请求对象
    /// </summary>
    IceApiRequest Request { get; }
    
    /// <summary>
    /// HTTP响应对象，中间件可以修改此对象
    /// </summary>
    IceApiResponse Response { get; set; }
    
    /// <summary>
    /// 用于在中间件之间传递数据的字典
    /// </summary>
    IDictionary<string, object> Items { get; }
    
    /// <summary>
    /// 标识请求是否被中止，不再继续执行后续中间件和业务逻辑
    /// </summary>
    bool IsAborted { get; set; }
    
    /// <summary>
    /// 当前执行的方法信息（安全封装，只暴露必要信息）
    /// </summary>
    IMethodInfo MethodInfo { get; }
    
    /// <summary>
    /// 控制器实例
    /// </summary>
    object Controller { get; }
    
    /// <summary>
    /// 当前环境信息
    /// </summary>
    Env Environment { get; }
}
```

### 2.2 方法信息接口

```csharp
/// <summary>
/// 方法信息接口，提供中间件所需的方法元数据
/// </summary>
public interface IMethodInfo
{
    /// <summary>
    /// 方法名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 获取指定类型的特性
    /// </summary>
    T GetAttribute<T>() where T : Attribute;
    
    /// <summary>
    /// 获取指定类型的所有特性（包括类级别和方法级别）
    /// </summary>
    T[] GetAttributes<T>() where T : Attribute;
    
    /// <summary>
    /// 获取自定义特性（支持类级别继承和方法级别覆盖）
    /// </summary>
    T[] GetCustomAttributes<T>() where T : Attribute;
}
```

### 2.3 中间件基础接口

```csharp
/// <summary>
/// 同步中间件基础接口
/// </summary>
public interface IPuffMiddleware
{
    /// <summary>
    /// 执行中间件逻辑（同步版本）
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="next">下一个中间件的执行委托</param>
    /// <returns>是否继续执行后续中间件</returns>
    bool Invoke(IPuffContext context, Action next);
}

/// <summary>
/// 同步中间件委托类型
/// </summary>
public delegate bool MiddlewareDelegate(IPuffContext context, Action next);
```

## 3. 特性驱动中间件系统

### 3.1 中间件特性基类

```csharp
/// <summary>
/// 中间件特性基类，所有中间件特性都应继承此类
/// </summary>
public abstract class BaseMiddlewareAttribute : Attribute
{
    /// <summary>
    /// 中间件执行优先级（数值越小优先级越高）
    /// </summary>
    public abstract int Priority { get; }
    
    /// <summary>
    /// 创建中间件实例
    /// </summary>
    /// <returns>中间件实例</returns>
    public abstract IPuffMiddleware CreateMiddleware();
}
```

### 3.2 业务中间件特性示例

```csharp
/// <summary>
/// 认证特性，标记需要认证的方法
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAuthAttribute : BaseMiddlewareAttribute
{
    /// <summary>
    /// 认证中间件优先级最高
    /// </summary>
    public override int Priority => 10;
    
    /// <summary>
    /// 创建认证中间件实例
    /// </summary>
    public override IPuffMiddleware CreateMiddleware()
    {
        return new TokenValidationMiddleware();
    }
}

/// <summary>
/// 限流特性，配置API限流策略
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
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
    /// 限流中间件优先级中等
    /// </summary>
    public override int Priority => 50;
    
    /// <summary>
    /// 创建限流中间件实例
    /// </summary>
    public override IPuffMiddleware CreateMiddleware()
    {
        return new RateLimitMiddleware();
    }
}

/// <summary>
/// 审计特性，记录API调用日志
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuditAttribute : BaseMiddlewareAttribute
{
    /// <summary>
    /// 审计操作名称
    /// </summary>
    public string Action { get; set; }
    
    /// <summary>
    /// 审计中间件优先级最低（在最后执行）
    /// </summary>
    public override int Priority => 90;
    
    /// <summary>
    /// 创建审计中间件实例
    /// </summary>
    public override IPuffMiddleware CreateMiddleware()
    {
        return new AuditMiddleware();
    }
}
```

## 4. 特性驱动中间件管道

### 4.1 管道接口

```csharp
/// <summary>
/// 中间件管道接口（简化版，无配置方法）
/// </summary>
public interface IMiddlewarePipeline
{
    /// <summary>
    /// 执行中间件管道（同步版本）
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="businessLogic">业务逻辑委托</param>
    /// <returns>是否成功执行</returns>
    bool Invoke(IPuffContext context, Action businessLogic);
}
```

### 4.2 特性驱动管道实现

```csharp
/// <summary>
/// 基于特性的同步中间件管道实现
/// </summary>
public class AttributeBasedMiddlewarePipeline : IMiddlewarePipeline
{
    public bool Invoke(IPuffContext context, Action businessLogic)
    {
        // 获取方法上的所有中间件特性
        var middlewareAttributes = context.MethodInfo.GetCustomAttributes<BaseMiddlewareAttribute>();
        
        if (middlewareAttributes == null || middlewareAttributes.Length == 0)
        {
            // 如果没有中间件特性，直接执行业务逻辑
            businessLogic();
            return true;
        }
        
        // 按优先级排序（数值越小优先级越高）
        var sortedAttributes = middlewareAttributes.OrderBy(attr => attr.Priority).ToArray();
        
        // 创建中间件实例列表
        var middlewares = new List<IPuffMiddleware>();
        foreach (var attr in sortedAttributes)
        {
            try
            {
                var middleware = attr.CreateMiddleware();
                if (middleware != null)
                {
                    middlewares.Add(middleware);
                }
            }
            catch (Exception ex)
            {
                Logger.Err($"创建中间件失败: {attr.GetType().Name}", ex.ToString());
                // 中间件创建失败时，继续处理其他中间件
            }
        }
        
        // 构建执行链
        var index = 0;
        var executionSuccessful = true;
        
        void InvokeNext()
        {
            if (context.IsAborted) 
            {
                executionSuccessful = false;
                return;
            }
            
            if (index >= middlewares.Count)
            {
                businessLogic();
                executionSuccessful = !context.IsAborted;
                return;
            }
            
            var middleware = middlewares[index++];
            
            // 执行中间件，并检查返回值
            var continueExecution = middleware.Invoke(context, () => {
                InvokeNext(); // Action调用，不返回值
            });
            
            // 如果中间件返回false，停止执行
            if (!continueExecution)
            {
                executionSuccessful = false;
                return;
            }
            
            // 检查执行状态
            if (!executionSuccessful)
            {
                return;
            }
        }
        
        InvokeNext();
        return executionSuccessful;
    }
}
```

## 5. JmController集成

### 5.1 扩展后的JmController

```csharp
public abstract class JmController : ControllerBase
{
    JmModule _jmod;

    /// <summary>
    /// 基于特性的中间件管道实例
    /// </summary>
    protected virtual IMiddlewarePipeline MiddlewarePipeline { get; } = new AttributeBasedMiddlewarePipeline();

    protected JmController()
    {
        _jmod = JmGlobal.Retrieve(GetType());
    }

    [Route("{_verb}")]
    [HttpGet]
    [HttpPost]
    public IceApiResponse _MethodDispatch()
    {
        const string InternalServerError = "InternalServerError";
        
        IceApiResponse response = null;
        var env = new Env(Request);
        
        try
        {
            var verb = (string)RouteData.Values["_verb"];
            JmMethod jm = _jmod.GetJmMethod(verb);
            if (jm == null)
            {
                Logger.Err("VerbNotFound", "Url=" + verb);
                WebGlobal.curEnv.stat = "VerbNotFound";
                response = IceApiResponse.Error("VerbNotFound", verb);
                return response;
            }

            // 创建中间件上下文
            var context = new PuffContext(env, jm, this, Request);
            
            // 执行中间件管道（如果没有配置中间件，这里是空操作）
            var shouldContinue = MiddlewarePipeline.Invoke(context, () =>
            {
                // 执行原有的业务逻辑（与之前完全一致）
                // 使用内部方法获取原始 JmMethod，保持框架内部的完整功能
                var puffContext = context as PuffContext;
                context.Response = JmWebInvoker.Invoke(puffContext?.InternalMethod ?? jm, this, Request);
            });
            
            if (!shouldContinue || context.IsAborted)
            {
                response = context.Response ?? IceApiResponse.Error("RequestAborted");
                return response;
            }
            
            response = context.Response;
        }
        catch (TargetInvocationException e)
        {
            // 保持原有的异常处理逻辑完全不变
            // ... 异常处理代码省略
        }
        catch (NutsException e)
        {
            // ... 异常处理代码省略
        }
        catch (Exception e)
        {
            // ... 异常处理代码省略
        }
        
        _AccessLog(env);
        return response;
    }
    
    // 保留原有的_AccessLog方法完全不变
    // ... 其他方法省略
}
```

### 5.2 中间件上下文实现

```csharp
/// <summary>
/// 同步中间件上下文的具体实现
/// </summary>
public class PuffContext : IPuffContext
{
    public string RequestId => Environment.reqId;
    public string ControllerName => Controller.GetType().Name;
    public string ActionName => MethodInfo.Name;
    public IceApiRequest Request { get; private set; }
    public IceApiResponse Response { get; set; }
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
    public bool IsAborted { get; set; }
    public IMethodInfo MethodInfo { get; private set; }
    public object Controller { get; private set; }
    public Env Environment { get; private set; }
    
    /// <summary>
    /// 内部方法引用，供框架内部使用
    /// </summary>
    internal JmMethod InternalMethod { get; private set; }
    
    public PuffContext(Env env, JmMethod method, object controller, HttpRequest httpRequest)
    {
        Environment = env;
        InternalMethod = method;
        MethodInfo = new JmMethodInfo(method);
        Controller = controller;
        Request = new AspncHttpRequest(httpRequest);
    }
}
```

## 6. 中间件实现示例

### 6.1 认证中间件

```csharp
/// <summary>
/// Token验证中间件
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
        return null;
    }
}
```

### 6.2 限流中间件

```csharp
/// <summary>
/// API限流中间件
/// </summary>
public class RateLimitMiddleware : IPuffMiddleware
{
    private static readonly Dictionary<string, List<DateTime>> _requestTimes = new();
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
```

### 6.3 审计中间件

```csharp
/// <summary>
/// 操作审计日志中间件
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
```

## 7. 使用示例

### 7.1 方法级别中间件

```csharp
/// <summary>
/// 基础中间件演示控制器
/// </summary>
public class MiddlewareExampleController : JmController
{
    /// <summary>
    /// 公开API - 无中间件
    /// </summary>
    [IceApi()]
    public object GetPublicData()
    {
        return new
        {
            message = "This is public data, no middleware required",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }

    /// <summary>
    /// 需要认证的API
    /// </summary>
    [IceApi()]
    [RequireAuth]
    public object GetUserData()
    {
        return new
        {
            message = "This is protected user data",
            userId = "123", // 从中间件上下文获取
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }

    /// <summary>
    /// 需要限流的API
    /// </summary>
    [IceApi()]
    [RateLimit(5)] // 每分钟最多5次请求
    public object GetLimitedData()
    {
        return new
        {
            message = "This is rate limited data",
            limit = "5 requests per minute",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }

    /// <summary>
    /// 需要审计的API
    /// </summary>
    [IceApi()]
    [Audit(Action = "GetSensitiveData")]
    public object GetSensitiveData()
    {
        return new
        {
            message = "This is sensitive data requiring audit",
            auditLevel = "high",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }

    /// <summary>
    /// 组合中间件API - 认证 + 审计
    /// </summary>
    [IceApi()]
    [RequireAuth]
    [Audit(Action = "GetSecureUserData")]
    public object GetSecureUserData()
    {
        return new
        {
            message = "This is secure user data with auth and audit",
            userId = "123",
            securityLevel = "high",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }
}
```

### 7.2 类级别中间件

```csharp
/// <summary>
/// 安全区域控制器 - 演示类级别中间件
/// 整个控制器需要认证和审计
/// </summary>
[RequireAuth]  // 🏢 类级别：整个控制器都需要认证
[Audit(Action = "SecureControllerAccess")]  // 🏢 类级别：整个控制器的操作都会被审计
public class SecureAreaController : JmController
{
    /// <summary>
    /// 继承类级别中间件：RequireAuth + Audit
    /// </summary>
    [IceApi()]
    public object GetSecureInfo()
    {
        return new
        {
            message = "This method inherits class-level auth + audit",
            area = "secure",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            userId = "123", // 从认证中间件获取
            status = "success"
        };
    }

    /// <summary>
    /// 继承类级别中间件 + 方法级别限流
    /// </summary>
    [IceApi()]
    [RateLimit(3)] // ⚡ 方法级别：加上限流
    public object GetCriticalData()
    {
        return new
        {
            message = "Class auth+audit + method rate limit",
            area = "critical",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            userId = "123", // 从认证中间件获取
            status = "success"
        };
    }

    /// <summary>
    /// 继承类级别中间件 + 方法级别审计（覆盖类级别审计）
    /// </summary>
    [IceApi()]
    [Audit(Action = "SpecificOperation")] // 📋 方法级别：覆盖类级别的审计配置
    public object PerformSpecificOperation()
    {
        return new
        {
            message = "Class auth + method-specific audit",
            operation = "specific",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            userId = "123", // 从认证中间件获取
            status = "success"
        };
    }
}
```

### 7.3 中间件执行优先级示例

```csharp
/// <summary>
/// 演示复杂中间件组合和优先级
/// </summary>
public class ComplexExampleController : JmController
{
    /// <summary>
    /// 多个中间件按优先级执行：认证(10) -> 限流(50) -> 审计(90)
    /// </summary>
    [IceApi()]
    [RequireAuth]      // Priority = 10 (最先执行)
    [RateLimit(10)]    // Priority = 50 (中间执行)
    [Audit(Action = "ComplexOperation")]  // Priority = 90 (最后执行)
    public object ComplexOperation()
    {
        return new
        {
            message = "Complex operation with multiple middleware",
            middlewares = new[] { "Auth", "RateLimit", "Audit" },
            executionOrder = "Auth -> RateLimit -> Audit -> Business Logic",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            status = "success"
        };
    }
}
```

## 8. 设计优势

### 8.1 技术优势
1. **零配置**：无需复杂的启动配置，开箱即用
2. **类型安全**：编译时确定中间件配置，避免运行时错误
3. **性能优秀**：同步执行，最小化运行时开销
4. **易于理解**：直接从方法签名看出使用的中间件
5. **优先级控制**：精确控制中间件执行顺序

### 8.2 开发体验优势
1. **声明式**：通过特性声明，代码即文档
2. **可组合**：中间件可以自由组合使用
3. **继承支持**：类级别特性自动应用到方法
4. **覆盖机制**：方法级别特性可以覆盖类级别配置
5. **向后兼容**：不影响现有代码，可渐进式采用

### 8.3 维护优势
1. **集中管理**：中间件逻辑与业务逻辑分离
2. **可测试**：每个中间件可以独立测试
3. **可扩展**：易于添加新的中间件类型
4. **统一接口**：所有中间件实现相同的接口

## 9. 实施建议

### 9.1 渐进式采用
1. **第一阶段**：为新功能添加中间件特性
2. **第二阶段**：为关键业务逻辑添加认证和审计
3. **第三阶段**：为高频API添加限流保护
4. **第四阶段**：根据需要添加更多业务中间件

### 9.2 最佳实践
1. **明确优先级**：为自定义中间件设置合理的Priority值
2. **简化逻辑**：中间件只关注单一职责
3. **错误处理**：合理处理中间件执行异常
4. **性能考虑**：避免在中间件中进行耗时操作
5. **测试覆盖**：为每个中间件提供完整的单元测试

### 9.3 扩展指南
1. **新增中间件特性**：继承`BaseMiddlewareAttribute`
2. **实现中间件逻辑**：实现`IPuffMiddleware`接口
3. **设置合理优先级**：根据业务需求设置Priority
4. **添加配置参数**：通过特性构造函数传递配置
5. **编写使用文档**：说明中间件的用途和配置方法

## 10. 总结

特性驱动的同步中间件系统实现了以下目标：

1. **简单易用**：通过特性声明，无需复杂配置
2. **类型安全**：编译时确定，避免运行时错误
3. **性能优秀**：同步执行，最小化开销
4. **扩展性强**：易于添加新的中间件类型
5. **向后兼容**：不影响现有代码结构

这种设计方式符合现代.NET开发习惯，提供了强大而简洁的中间件扩展能力，是Puff框架的重要组成部分。