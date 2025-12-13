# Puff Web Framework 使用文档

## 1. 简介
Puff 是一个基于 ASP.NET Core 的轻量级 Web 框架，旨在提供高性能、简单易用的 API 开发体验。它采用**特性驱动（Attribute-Driven）**的设计理念，通过 `JmController` 基类和 `[IceApi]` 特性，实现路由分发、参数绑定、结果格式化和中间件拦截。

### 主要特性
- **简洁的 API 定义**：通过 `[IceApi]` 特性快速定义接口
- **灵活的参数绑定**：自动从 Body/Query/Cookie 解析参数
- **同步中间件系统**：基于特性的拦截器机制
- **Swagger 集成**：自动生成 API 文档
- **SignalR 支持**：WebSocket 实时通信能力

## 2. 快速开始

### 2.1 创建控制器
所有控制器需继承自 `JmController`，并标记 `[Route]` 特性以定义基础路由。

```csharp
using Puff.NetCore;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
public class MyController : JmController
{
    // 定义 API 方法（注意：不要使用 public 修饰符）
    [IceApi]
    object Hello(string name)
    {
        return new { Message = $"Hello, {name}!" };
    }
}
```

> ⚠️ **注意**：`[IceApi]` 方法**不要**声明为 `public`，否则会与 Swagger 的标准 API 探索器冲突。Puff 使用反射调用方法，非 public 方法也能正常工作。

### 2.2 定义 API 接口 (`[IceApi]`)
使用 `[IceApi]` 特性将方法暴露为 API 接口。框架会根据方法名自动映射路由（例如 `POST /My/Hello`）。

#### 2.2.1 基础用法
```csharp
[IceApi]
void Ping() { } // 无返回值

[IceApi]
int Add(int x, int y) => x + y; // 返回基本类型

[IceApi]
object Echo(string name, int count) => new { name, count }; // 返回对象
```

#### 2.2.2 多值返回 (Tuple 支持)
支持 C# Tuple 返回多个值，框架会自动从 ValueTuple 推导字段名。

```csharp
// 返回 JSON: { "x": 2, "y": 1, "stat": "ok" }
[IceApi]
(int x, int y) Swap(int x, int y) => (y, x);

// 手动指定返回字段名
[IceApi(Ret = "name,value")]
(string n, int v) Cookie(string name, int value) => (name, value);
```

#### 2.2.3 IceApiFlag 模式

通过 `Flags` 属性控制参数解析和返回格式：

| Flag | 输入 | 输出 | 适用场景 |
|------|------|------|----------|
| `IceApiFlag.Json` (默认) | JSON Body + Query + Cookie | 自动 JSON 序列化 | 常规 RPC 接口 |
| `IceApiFlag.JsonIn` | JSON Body + Query + Cookie | 自定义 `IceApiResponse` | 需要自定义响应（文本、文件下载等）|
| `IceApiFlag.Http` | `IceApiRequest` 参数 | 自定义 `IceApiResponse` | 完全接管 HTTP 请求处理 |

```csharp
// IceApiFlag.Json (默认) - 自动处理输入输出
[IceApi]
object GetUser(int id) => new { Id = id, Name = "test" };

// IceApiFlag.JsonIn - 自定义响应
[IceApi(Flags = IceApiFlag.JsonIn)]
IceApiResponse Download(string filename)
{
    var response = IceApiResponse.String("file content");
    response.SetToSave(filename);  // 设置为文件下载
    return response;
}

// IceApiFlag.Http - 完全接管请求
[IceApi(Flags = IceApiFlag.Http)]
IceApiResponse RawHandler(IceApiRequest request)  // ⚠️ 必须使用 IceApiRequest 参数
{
    var body = ReadBody(request.GetStream());
    return new IceApiResponse { Text = "OK", HttpStatusCode = 200 };
}
```

> ⚠️ **注意**：使用 `IceApiFlag.Http` 时，方法签名**必须**是 `IceApiResponse MethodName(IceApiRequest request)`，否则框架会报错。

## 3. 参数绑定

### 3.1 参数来源与优先级

框架按以下优先级获取参数值：

```
JSON Body > Cookie > QueryString > 默认值
```

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 (最高) | JSON Body | POST 请求的 JSON 数据 |
| 2 | Cookie | HTTP Cookie |
| 3 | QueryString | URL 查询参数 |
| 4 (最低) | 默认值 | 方法参数的默认值 |

```csharp
// 如果 Body 中有 name，使用 Body 的值
// 否则从 Cookie/QueryString 获取
// 都没有则使用默认值 "Guest"
[IceApi]
object Greet(string name = "Guest") => new { Message = $"Hello, {name}" };
```

### 3.2 特殊参数类型

框架支持自动注入以下特殊类型：

```csharp
[IceApi]
object GetInfo(HttpRequest request)  // 自动注入 ASP.NET Core HttpRequest
{
    return new { Host = request.Host.Value };
}
```

## 4. 请求上下文 (Env)

通过 `WebGlobal.curEnv` 访问当前请求的环境信息。

### 4.1 Env 对象属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `reqId` | `string` | 请求唯一标识符（Base64 编码的 GUID） |
| `ip` | `string` | 客户端 IP（支持 X-Forwarded-For） |
| `startTime` | `long` | 请求开始时间戳（UTC 毫秒） |
| `host` | `string` | 请求路径 |
| `postStr` | `string` | 原始 POST Body 字符串 |
| `queryStr` | `string` | Query + Cookie 的 JSON 字符串 |
| `stat` | `string` | 响应状态码（用于日志） |
| `request` | `HttpRequest` | 原始 ASP.NET Core 请求对象 |
| `logParams` | `Dictionary<string, JsonNode>` | 自定义日志参数 |

### 4.2 使用示例

```csharp
[IceApi]
object GetContext()
{
    var env = WebGlobal.curEnv;
    return new {
        RequestId = env.reqId,
        ClientIP = env.ip,
        StartTime = env.startTime,
        Path = env.host
    };
}
```

### 4.3 添加自定义日志参数

```csharp
[IceApi]
object ProcessOrder(int orderId)
{
    var env = WebGlobal.curEnv;
    
    // ⚠️ 注意：key 不能重复，否则会抛出异常
    // 建议使用业务前缀避免冲突，如 "order_id" 而不是 "id"
    if (!env.logParams.ContainsKey("order_id"))
    {
        env.AddLogParam("order_id", DObject.New(orderId).ToJson());
    }
    
    return new { OrderId = orderId, Status = "processed" };
}
```

> ⚠️ **注意**：`AddLogParam` 内部使用 `Dictionary.Add()`，如果 key 已存在会抛出异常。建议：
> 1. 使用具有业务含义的唯一 key（如 `user_id`、`order_id`）
> 2. 添加前检查 key 是否已存在

## 5. 中间件系统 (Interceptor)

Puff 提供了一套**特性驱动的同步中间件系统**，允许通过在类或方法上标记特性来拦截请求。

### 5.1 使用中间件
只需在控制器类或方法上添加对应的中间件特性。

```csharp
public class SecureController : JmController
{
    // 标记需要认证
    [RequireAuth] 
    [IceApi]
    object UserInfo()
    {
        var userId = WebGlobal.curEnv.logParams["UserId"];
        return new { UserId = userId };
    }

    // 组合使用：认证 + 限流
    [RequireAuth]
    [RateLimit(10)] // 每分钟 10 次
    [IceApi]
    void SensitiveAction() { }
}

// 类级别中间件：整个控制器都需要认证
[RequireAuth]
[Audit(Action = "SecureAccess")]
public class AdminController : JmController
{
    [IceApi]
    object Dashboard() => new { Status = "OK" };
}
```

### 5.2 自定义中间件
自定义中间件需包含两部分：**特性定义**和**中间件逻辑实现**。

#### Step 1: 定义特性
继承 `BaseMiddlewareAttribute`。

```csharp
public class MyLogAttribute : BaseMiddlewareAttribute
{
    public override int Priority => 50; // 优先级，越小越先执行

    public override IPuffMiddleware CreateMiddleware()
    {
        return new MyLogMiddleware();
    }
}
```

#### Step 2: 实现逻辑
实现 `IPuffMiddleware` 接口。

```csharp
public class MyLogMiddleware : IPuffMiddleware
{
    public bool Invoke(IPuffContext context, Action next)
    {
        Console.WriteLine($"Before: {context.ActionName}");
        
        // 执行下一个中间件或业务逻辑
        next(); 
        
        Console.WriteLine("After");
        return true; // 返回 true 表示继续执行
    }
}
```

### 5.3 中间件优先级
多个中间件按 `Priority` 从小到大执行。建议的优先级范围：

| 优先级 | 用途 |
|--------|------|
| 0-20 | 认证、权限检查 |
| 21-50 | 限流、防重放 |
| 51-80 | 日志、审计 |
| 81-100 | 其他处理 |

## 6. Swagger 集成

Puff 提供了与 Swagger 的深度集成，自动生成 API 文档。

### 6.1 启用 Swagger

在 `Startup.cs` 中配置：

```csharp
using Puff.NetCore.Swagger;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ... 其他配置 ...
        
        // 注册 Puff Swagger
        services.AddPuffSwagger("My API", "v1");
    }

    public void Configure(IApplicationBuilder app)
    {
        // 启用 Swagger UI
        app.UsePuffSwagger();
        
        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
        });
    }
}
```

### 6.2 启用 XML 注释

在项目 `.csproj` 中添加：

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

然后在代码中添加 XML 注释：

```csharp
/// <summary>
/// 用户管理控制器
/// </summary>
[Route("[controller]")]
public class UserController : JmController
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    [IceApi]
    object GetUser(int id) => new { Id = id };
}
```

### 6.3 访问 Swagger UI

启动应用后访问：`http://localhost:{port}/swagger/index.html`

## 7. SignalR/WebSocket 集成

Puff 支持 SignalR 实现 WebSocket 实时通信。

### 7.1 创建 Hub

```csharp
using Microsoft.AspNetCore.SignalR;

public interface IChatClient
{
    Task ReceiveMessage(string user, string message);
    Task ReceiveSystemNotification(string message);
}

public class ChatHub : Hub<IChatClient>
{
    // 连接建立
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[ChatHub] Client connected: {Context.ConnectionId}");
        await Clients.All.ReceiveSystemNotification($"User {Context.ConnectionId} connected");
        await base.OnConnectedAsync();
    }

    // 连接断开
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        Console.WriteLine($"[ChatHub] Client disconnected: {Context.ConnectionId}");
        await Clients.All.ReceiveSystemNotification($"User {Context.ConnectionId} disconnected");
        await base.OnDisconnectedAsync(exception);
    }

    // 发送消息
    public async Task SendMessage(string user, string message)
    {
        Console.WriteLine($"[ChatHub] {user}: {message}");
        await Clients.All.ReceiveMessage(user, message);
    }

    // 加入房间
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).ReceiveSystemNotification($"User joined {roomName}");
    }
}
```

### 7.2 配置 SignalR

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSignalR().AddHubOptions<ChatHub>(options => {
        options.AddFilter<PuffHubFilter>();  // 可选：添加 Puff Hub 过滤器
    });
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
        endpoints.MapHub<ChatHub>("/chatHub");  // 映射 Hub 路由
    });
}
```

### 7.3 从 Controller 调用 Hub

```csharp
[Route("[controller]")]
public class SystemController : JmController
{
    private readonly IHubContext<ChatHub, IChatClient> _chatHub;

    public SystemController(IHubContext<ChatHub, IChatClient> chatHub)
    {
        _chatHub = chatHub;
    }

    [IceApi(Flags = IceApiFlag.JsonIn)]
    IceApiResponse Broadcast(string message)
    {
        _chatHub.Clients.All.ReceiveSystemNotification($"[Admin]: {message}");
        return IceApiResponse.String("Broadcast sent");
    }
}
```

## 8. 异常处理

框架内置了异常捕获机制。
- **业务异常**: 抛出 `NutsException`，框架会返回对应的 Error Code 和 Message。
- **系统异常**: 捕获其他异常，记录日志并返回 "InternalServerError"。

```csharp
[IceApi]
void Check(int value)
{
    if (value < 0)
        throw new NutsException("InvalidValue", "Value must be positive");
}
```

响应示例：
```json
{ "stat": "InvalidValue", "m": "Value must be positive" }
```

## 9. 注意事项汇总

### 9.1 IceApi 方法定义

| ❌ 错误 | ✅ 正确 | 说明 |
|---------|---------|------|
| `public object Method()` | `object Method()` | 不要使用 public，会与 Swagger 冲突 |
| `[IceApi(Flags = IceApiFlag.Http)] void M(string s)` | `[IceApi(Flags = IceApiFlag.Http)] IceApiResponse M(IceApiRequest req)` | Http 模式必须使用 IceApiRequest |

### 9.2 参数绑定

- **优先级**：Body > Cookie > QueryString > 默认值
- **同名参数**：如果多个来源有相同的参数名，按优先级取值
- **HttpRequest 注入**：可以在参数中声明 `HttpRequest request`，框架会自动注入

### 9.3 日志参数

```csharp
// ❌ 可能抛出 key 重复异常
env.AddLogParam("id", value);

// ✅ 安全写法
if (!env.logParams.ContainsKey("order_id"))
{
    env.AddLogParam("order_id", value);
}

// ✅ 或使用 TryAdd（如果使用支持的 .NET 版本）
env.logParams.TryAdd("order_id", value);
```

### 9.4 端口选择

避免使用被浏览器阻止的"不安全端口"：

| 端口 | 说明 |
|------|------|
| 6000 | X11 - 被 Chrome/Edge 阻止 |
| 6665-6669 | IRC - 被阻止 |

建议使用：5000、5001、8080、9000 等常用端口。

## 10. 全局对象与工具

| 对象 | 说明 |
|------|------|
| `WebGlobal.curEnv` | 当前请求的环境上下文（线程静态） |
| `Logger` | 内置日志工具 |
| `SentryUtil` | 集成 Sentry 错误上报 |

```csharp
// 获取当前请求 ID（用于日志追踪）
var reqId = WebGlobal.curEnv.reqId;

// 获取客户端 IP
var clientIp = WebGlobal.curEnv.ip;
```
