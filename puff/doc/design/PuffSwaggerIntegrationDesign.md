# PuffNetCore Swagger 集成设计方案

## 1. 背景与挑战

PuffNetCore 是一个基于 ASP.NET Core 的轻量级框架，其核心机制是利用 `JmController` 的 `_MethodDispatch` 方法进行统一的路由分发。

```csharp
[Route("{_verb}")]
public IceApiResponse _MethodDispatch() { ... }
```

这种**动态分发机制**导致标准的 Swagger 生成器（如 Swashbuckle）无法正确识别业务 API。
- **现状**：Swagger 只会生成一个通用的 `POST /{controller}/{_verb}` 接口。
- **目标**：Swagger 能够扫描出所有标记了 `[IceApi]` 的具体方法（如 `/My/Echo`, `/My/Ping`），并正确展示参数和返回值结构。

## 2. 总体架构设计

为了不侵入现有的 Puff 运行时逻辑，我们采用 **Swashbuckle 扩展机制**。通过实现自定义的 `IDocumentFilter`，在 Swagger 文档生成过程的最后阶段，利用 Puff 现有的元数据（`JmGlobal` / `JmModule`）动态注入 API 定义。

### 架构图
```mermaid
graph TD
    A[ASP.NET Core Startup] --> B[AddSwaggerGen]
    B --> C[Swashbuckle Generator]
    C --> D[Standard Scraper]
    D -->|只发现 _MethodDispatch| E[OpenApiDocument (Incomplete)]
    E --> F[PuffDocumentFilter]
    
    subgraph "Puff Metadata Extension"
    F --> G[Reflection: Scan JmController Types]
    G --> H[JmGlobal.Retrieve: Get JmModule]
    H --> I[Iterate JmMethods]
    I --> J[Generate OpenApiPathItem]
    end
    
    J -->|Inject Paths| K[Final OpenApiDocument]
    K --> L[Swagger UI]
```

## 3. 详细设计

### 3.1 核心组件：`PuffDocumentFilter`

这是集成的核心类，实现 `IDocumentFilter` 接口。

**职责：**
1.  **移除干扰项**：从文档中移除 `_MethodDispatch` 这个内部路由。
2.  **扫描控制器**：查找所有继承自 `JmController` 的类。
3.  **构建路径**：遍历控制器下的 `[IceApi]` 方法，构建具体的 URL 路径（例如 `/My/Echo`）。
4.  **映射参数**：解析方法参数，生成对应的 Request Body Schema（Puff 默认将参数封装为 JSON 对象）。
5.  **映射响应**：根据 `[IceApi]` 的 `Ret` 属性或方法返回值，生成 Response Schema。

### 3.2 参数映射策略 (Request Mapping)

Puff 的默认行为是将所有参数包装成一个 JSON 对象（除非是 `IceApiRequest`）。

**示例方法：**
```csharp
[IceApi]
public object Echo(string name, int count) { ... }
```

**Swagger Schema 生成逻辑：**
- **Body Content-Type**: `application/json`
- **Schema**:
  ```json
  {
      "type": "object",
      "properties": {
          "name": { "type": "string" },
          "count": { "type": "integer" }
      }
  }
  ```

### 3.3 响应映射策略 (Response Mapping)

Puff 的返回值处理较为灵活，特别是支持 Tuple 和 `Ret` 属性重命名。

**示例方法：**
```csharp
[IceApi(Ret = "uid,uname")]
public (int id, string name) GetUser() { ... }
```

**Swagger Schema 生成逻辑：**
- **Body Content-Type**: `application/json`
- **Schema**:
  ```json
  {
      "type": "object",
      "properties": {
          "uid": { "type": "integer" },
          "uname": { "type": "string" }
      }
  }
  ```
- **特殊情况**：如果是 `IceApiResponse`，则默认为通用 JSON 结构。

## 4. 工程实现与目录结构

为了保持 PuffNetCore 的整洁性，我们将 Swagger 集成功能封装在一个**独立的扩展目录**中，而不是混合在核心逻辑里。

### 4.1 框架侧目录调整 (PuffNetCore Project)

```text
/PuffNetCore
├── /Swagger                    # [新增] Swagger 集成模块
│   ├── PuffDocumentFilter.cs   # [核心] 实现 IDocumentFilter，动态注入 API 定义
│   ├── SchemaBuilder.cs        # [辅助] 负责构建 Request/Response 的 JSON Schema
│   └── SwaggerExtensions.cs    # [扩展] 提供 AddPuffSwagger() 扩展方法
├── JmController.cs             # (保持不变)
└── PuffNetCore.csproj          # [修改] 增加 Swashbuckle.AspNetCore 依赖
```

### 4.2 核心实现 (PuffDocumentFilter)

**PuffDocumentFilter.cs** 将是核心实现。它利用反射扫描当前程序集中所有 `JmController` 子类，并读取 `[IceApi]` 特性。

```csharp
namespace Puff.NetCore.Swagger
{
    public class PuffDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 1. 扫描所有 JmController
            // ... (逻辑同上文) ...
            
            // 2. 注入 Path
            // ...
            
            // 3. 清理生成的通用路由
            var keysToRemove = swaggerDoc.Paths.Keys.Where(k => k.Contains("{_verb}")).ToList();
            foreach (var key in keysToRemove) swaggerDoc.Paths.Remove(key);
        }
    }
}
```

### 4.3 扩展方法 (SwaggerExtensions)

为了简化上游应用的使用，我们提供一个一行代码的扩展方法。

```csharp
public static class PuffSwaggerExtensions
{
    public static void AddPuffSwagger(this IServiceCollection services, string title = "Puff API", string version = "v1")
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
            
            // 自动注册核心过滤器
            c.DocumentFilter<PuffDocumentFilter>();
            
            // 如果需要 XML 注释支持，可以在这里自动加载
            var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
    }

    public static void UsePuffSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Puff API v1");
            c.RoutePrefix = "swagger"; 
        });
    }
}
```

## 5. 上游应用集成指南

上游应用（例如 `TestAspNetCore`）只需极少的改动即可启用 Swagger。

### 5.1 修改 `Startup.cs`

利用上面封装的扩展方法，集成变得非常简单：

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ... 原有配置 ...
        
        // 1. [新增] 注册 Puff Swagger
        services.AddPuffSwagger("My Project API", "v1");
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        // ... 原有配置 ...

        // 2. [新增] 启用 Swagger UI
        app.UsePuffSwagger();
        
        app.UseRouting();
        app.UseEndpoints(endpoints => { ... });
    }
}
```

### 5.2 启用 XML 文档 (可选)

为了让 Swagger 显示方法的 `<summary>` 注释，需要在应用项目的 `.csproj` 中启用 XML 生成：

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

## 6. 待解决细节

1.  **JmModule 可访问性**：目前 `JmModule.Map` 是私有的，需要改为 `public` 或提供访问器，以便 Filter 高效读取，避免重复反射。
2.  **XML 注释支持**：Filter 需要手动读取 XML Documentation 文件，将 `<summary>` 填充到 Swagger 的 Description 中。
3.  **复杂类型支持**：对于输入参数是复杂对象（DTO）的情况，Filter 需要利用 Swagger 的 SchemaGenerator 来生成引用类型定义。

## 6. 预期效果
 Swagger UI 将不再显示含糊的 `_MethodDispatch`，而是清晰列出：
 - `POST /My/Echo`
 - `POST /My/Ping`
 - `POST /My/Swap`
 
 且参数输入框能正确提示 JSON 结构。
