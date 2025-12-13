# Puff Framework + ASP.NET Core SignalR 深度集成方案

## 1. 方案概述

本方案旨在为 Puff 框架增加实时全双工通信能力，通过集成 **ASP.NET Core SignalR**，实现 "HTTP RPC + WebSocket Push" 的双模架构。

- **核心目标**:
    1. **统一上下文**: 无论是 HTTP 还是 WebSocket 请求，都能获取到 Puff 标准的 `Env` 上下文（ReqId, IP, LogParams）。
    2. **互通性**: HTTP 接口 (`JmController`) 可以轻松向 WebSocket 客户端推送消息。
    3. **业务复用**: 保持 Puff 的轻量级风格，业务逻辑与通信协议解耦。

## 2. 核心组件设计

### 2.1 基础设施改造 (Prerequisite)

由于 SignalR 是基于 `async/await` 的全异步模型，而 Puff 原有 `WebGlobal.curEnv` 使用 `[ThreadStatic]`，在异步上下文切换后会导致上下文丢失。
**强烈建议**将 `WebGlobal` 改造为 `AsyncLocal`：

```csharp
// PuffNetCore/JmController.cs (建议修改)
public class WebGlobal
{
    private static AsyncLocal<Env> _curEnv = new AsyncLocal<Env>();
    
    public static Env curEnv
    {
        get => _curEnv.Value;
        set => _curEnv.Value = value;
    }
}
```

### 2.2 SignalR 适配器 (PuffHubFilter)

这是连接 SignalR 与 Puff 的桥梁。它充当 SignalR 的中间件，负责在每个 Hub 方法调用前初始化 Puff 环境 (`Env`)。

```csharp
using Microsoft.AspNetCore.SignalR;
using Puff.NetCore;
using System.Threading.Tasks;

public class PuffHubFilter : IHubFilter
{
    public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        // 1. 获取 HttpContext (SignalR 基于 HTTP 握手，通常可以获取到)
        var httpContext = invocationContext.Context.GetHttpContext();
        if (httpContext == null)
        {
            // 非 HTTP 传输时的降级处理
            return await next(invocationContext);
        }

        // 2. 初始化 Puff Env
        // 注意：Env 构造函数会自动赋值给 WebGlobal.curEnv
        // 如果已改造为 AsyncLocal，此处即可跨 await 保持
        var env = new Env(httpContext.Request);
        
        // 补充 SignalR 特有日志参数
        env.AddLogParam("Transport", "SignalR");
        env.AddLogParam("HubMethod", invocationContext.HubMethodName);
        env.AddLogParam("ConnectionId", invocationContext.Context.ConnectionId);

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
            // 5. 访问日志 (可选)
            // _AccessLog(env); 
        }
    }
}
```

## 3. 完整 Chat WebSocket 实例

### 3.1 强类型客户端接口定义

定义服务端能向客户端发送的消息契约，避免魔术字符串。

```csharp
public interface IChatClient
{
    // 接收普通消息
    Task ReceiveMessage(string user, string message);
    
    // 接收系统通知
    Task ReceiveSystemNotification(string notification);
}
```

### 3.2 ChatHub 实现

这是 WebSocket 的服务端入口，类似于 `JmController`。

```csharp
using Microsoft.AspNetCore.SignalR;
using Puff.NetCore;
using System.Threading.Tasks;

public class ChatHub : Hub<IChatClient>
{
    // 客户端调用服务端: connection.invoke("SendMessage", user, message)
    public async Task SendMessage(string user, string message)
    {
        // 1. 获取 Puff 上下文（由 Filter 注入）
        var env = WebGlobal.curEnv;
        
        // 2. 记录业务日志
        Logger.Info($"[{env.reqId}] User {user} sending message via WebSocket");

        // 3. 广播给所有客户端
        await Clients.All.ReceiveMessage(user, message);
    }

    // 客户端加入分组: connection.invoke("JoinRoom", roomName)
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).ReceiveSystemNotification($"User {Context.ConnectionId} joined {roomName}");
    }
}
```

### 3.3 HTTP 接口推送 (Controller -> WebSocket)

演示如何在传统的 HTTP 接口中，触发 WebSocket 推送。

```csharp
[Route("[controller]")]
[ApiController]
public class SystemController : JmController
{
    private readonly IHubContext<ChatHub, IChatClient> _chatHub;

    // 注入 HubContext
    public SystemController(IHubContext<ChatHub, IChatClient> chatHub)
    {
        _chatHub = chatHub;
    }

    // HTTP API: POST /System/Broadcast
    [IceApi(Flags = IceApiFlag.JsonIn)]
    public IceApiResponse Broadcast(string message)
    {
        // 业务逻辑处理...
        
        // 通过 WebSocket 推送给所有在线用户
        // 注意：这里不需要 await 等待推送完成，除非需要确保送达
        _chatHub.Clients.All.ReceiveSystemNotification($"[System Admin]: {message}");

        return IceApiResponse.String("Broadcast sent");
    }
}
```

## 4. 启动配置 (Startup.cs)

在 `ConfigureServices` 和 `Configure` 中注册 SignalR。

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ... 原有配置 ...

        // 1. 注册 SignalR 并添加 Puff 过滤器
        services.AddSignalR(options =>
        {
            options.AddFilter<PuffHubFilter>();
        });
        
        // 如果需要跨域（WebSocket常见需求）
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.WithOrigins("http://localhost:8080") // 前端地址
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials(); // SignalR 需要凭证
            });
        });
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        // ... 原有配置 ...

        app.UseCors("CorsPolicy");

        // 2. 映射 Hub 路由
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<ChatHub>("/chatHub"); // 映射 WebSocket 地址
        });
    }
}
```

## 5. 前端调用示例 (JavaScript)

使用 `@microsoft/signalr` 库进行连接。

```javascript
const signalR = require("@microsoft/signalr");

// 1. 建立连接
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chatHub")
    .withAutomaticReconnect()
    .build();

// 2. 注册客户端方法 (对应 IChatClient)
connection.on("ReceiveMessage", (user, message) => {
    console.log(`${user} says: ${message}`);
    // 更新 UI...
});

connection.on("ReceiveSystemNotification", (notification) => {
    console.warn(`SYSTEM: ${notification}`);
});

// 3. 启动连接
async function start() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        
        // 4. 调用服务端方法
        await connection.invoke("SendMessage", "ClientA", "Hello Puff!");
    } catch (err) {
        console.error(err);
        setTimeout(start, 5000);
    }
}

start();
```

## 6. 下游应用集成指南

### 6.1 服务拆分模式
为了避免在 Controller 和 Hub 中重复编写业务逻辑，建议提取 **Service 层**。

```csharp
// 业务逻辑接口
public interface IChatService
{
    void ProcessMessage(string user, string msg);
}

// 业务逻辑实现
public class ChatService : IChatService 
{
    // 这里也可以注入 IHubContext 用于推送
    public void ProcessMessage(string user, string msg)
    {
        // 1. 敏感词过滤
        // 2. 存入数据库
        // 3. 触发其他事件
    }
}
```

### 6.2 在 Hub 中使用 Service

```csharp
public class ChatHub : Hub<IChatClient>
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendMessage(string user, string message)
    {
        // 调用业务层
        _chatService.ProcessMessage(user, message);
        
        // 转发消息
        await Clients.All.ReceiveMessage(user, message);
    }
}
```

## 7. 框架目录结构变更 (Framework Layer)

为支持 WebSocket 集成，**PuffNetCore** 框架本身需要进行如下目录结构调整和文件新增：

```text
/PuffNetCore                    # 框架源码根目录
├── /SignalR                    # [新增] SignalR 集成专用模块
│   ├── PuffHubFilter.cs        # [新增] 核心适配器，负责将 SignalR 上下文转换为 Puff Env
│   └── PuffBaseHub.cs          # [新增] (可选) Hub 基类，提供便捷属性访问 Env
├── JmController.cs             # [修改] 将 WebGlobal.curEnv 升级为 AsyncLocal<Env>
├── Middleware.cs               # 中间件定义 (保持不变)
├── Formatter.cs                # 格式化器 (保持不变)
└── PuffNetCore.csproj          # [修改] 增加 Microsoft.AspNetCore.SignalR 依赖 (如需)
```

### 关键文件说明
1.  **`/SignalR/PuffHubFilter.cs`**: 
    *   **作用**: 实现 `IHubFilter` 接口。
    *   **职责**: 拦截所有 Hub 调用，从 `HubInvocationContext` 中提取 `HttpContext`，初始化 `WebGlobal.curEnv`，确保后续业务逻辑能获取到正确的上下文。
2.  **`JmController.cs`**:
    *   **变更**: 必须将 `WebGlobal.curEnv` 的存储方式从 `[ThreadStatic]` 修改为 `AsyncLocal<T>`。这是因为 SignalR 严重依赖 `async/await`，线程切换频繁，`ThreadStatic` 会导致上下文丢失。

## 8. 推荐项目目录结构（应用层）

此结构展示了一个基于 Puff 框架的**下游业务应用**（如聊天服务）的典型目录组织。请注意，`/Services`、`/Hubs` 和 `/Controllers` 均属于业务应用代码，而非框架底层代码。

```text
/MyChatApp                      # 业务应用根目录
├── /Controllers                # HTTP API 层 (基于 Puff JmController)
│   ├── SystemController.cs     # 业务控制器 (注入 IHubContext 实现推送)
│   └── ...
├── /Hubs                       # WebSocket 协议层 (基于 SignalR)
│   ├── /Interfaces             # 强类型客户端接口定义
│   │   └── IChatClient.cs
│   ├── ChatHub.cs              # SignalR Hub 实现
│   └── PuffHubFilter.cs        # 协议适配器 (通常复制到项目中或封装在库中)
├── /Services                   # 领域业务层 (纯业务逻辑，不依赖 HTTP/WS 上下文)
│   ├── IChatService.cs         # 业务接口
│   └── ChatService.cs          # 业务实现 (被 Controller 和 Hub 共享调用)
├── /Models                     # 数据模型 / DTO
│   └── ...
├── /PuffNetCore                # (引用) Puff 框架核心源码或 NuGet 包
│   ├── JmController.cs
│   └── ...
├── Startup.cs                  # 启动配置 (DI 注册 Services, Hubs 等)
└── Program.cs
```

### 目录说明
1.  **`/Services` (核心业务)**: 
    *   **定义者**: 具体的业务应用（如 Chat App）。
    *   **职责**: 包含核心业务逻辑（如消息持久化、敏感词过滤）。
    *   **原则**: **独立于传输协议**。它不应直接依赖 `HttpContext` 或 `HubContext`，而是通过参数接收数据。这使得同一套逻辑可以被 HTTP API 和 WebSocket 同时复用。
2.  **`/Hubs` (WebSocket 接入层)**: 
    *   处理 WebSocket 连接、消息路由和实时推送。它只负责协议转换，收到消息后应立即调用 `/Services` 处理业务。
3.  **`/Controllers` (HTTP 接入层)**: 
    *   处理常规 HTTP 请求。同样，它只负责 HTTP 协议解析，核心逻辑委托给 `/Services`。

## 8. 总结

| 组件 | 角色 | 职责 |
| :--- | :--- | :--- |
| **PuffHubFilter** | 适配器 | 将 HTTP Context 转换为 Puff `Env`，实现日志和上下文统一。 |
| **IHubContext<T>** | 推送器 | 允许 HTTP Controller 主动向 WebSocket 客户端推送消息。 |
| **ChatHub** | 网关 | WebSocket 协议入口，处理连接管理和消息路由。 |
| **Service Layer** | 核心 | 承载具体的业务逻辑，供 Controller 和 Hub 共同调用。 |

通过此方案，Puff 框架不仅保留了原有的高效同步 RPC 能力，还扩展了现代化的实时通信能力，且两者共享同一套上下文和日志系统。
