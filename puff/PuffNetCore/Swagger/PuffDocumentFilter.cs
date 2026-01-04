using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Puff.Marshal;
using Microsoft.AspNetCore.Mvc;

namespace Puff.NetCore.Swagger
{
    public class PuffDocumentFilter : IDocumentFilter
    {
        /// <summary>
        /// 不需要反射字段的框架类型 - 只显示类型名称
        /// </summary>
        private static readonly HashSet<Type> NoReflectTypes = new HashSet<Type>
        {
            typeof(HttpRequest),
            typeof(HttpContext),
            typeof(HttpResponse),
            typeof(IceApiRequest),
            typeof(IceApiResponse),
        };

        /// <summary>
        /// 框架类型的简要描述
        /// </summary>
        private static readonly Dictionary<Type, string> FrameworkTypeDescriptions = new Dictionary<Type, string>
        {
            { typeof(HttpRequest), "ASP.NET Core HTTP 请求对象（由框架自动注入）" },
            { typeof(HttpContext), "ASP.NET Core HTTP 上下文对象（由框架自动注入）" },
            { typeof(HttpResponse), "ASP.NET Core HTTP 响应对象" },
            { typeof(IceApiRequest), "Puff 框架请求对象（包含 Headers、Cookies、Query、Body 等）" },
            { typeof(IceApiResponse), "Puff 框架响应对象（可返回 JSON、文本、二进制数据或文件）" },
        };

        /// <summary>
        /// XML 文档缓存
        /// </summary>
        private static readonly Dictionary<Assembly, XDocument> XmlDocCache = new Dictionary<Assembly, XDocument>();

        /// <summary>
        /// 获取程序集的 XML 文档
        /// </summary>
        private static XDocument GetXmlDoc(Assembly assembly)
        {
            if (XmlDocCache.TryGetValue(assembly, out var doc))
                return doc;

            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            
            if (File.Exists(xmlPath))
            {
                try
                {
                    doc = XDocument.Load(xmlPath);
                    XmlDocCache[assembly] = doc;
                    return doc;
                }
                catch
                {
                    // 忽略 XML 加载错误
                }
            }

            XmlDocCache[assembly] = null;
            return null;
        }

        /// <summary>
        /// 从 XML 文档中获取方法的 summary 注释
        /// </summary>
        private static string GetMethodSummary(MethodInfo method)
        {
            var doc = GetXmlDoc(method.DeclaringType.Assembly);
            if (doc == null) return null;

            // 构建 XML 文档中的成员名称
            // 格式: M:Namespace.ClassName.MethodName(ParamType1,ParamType2)
            var typeName = method.DeclaringType.FullName.Replace("+", ".");
            var paramTypes = string.Join(",", method.GetParameters().Select(p => GetXmlTypeName(p.ParameterType)));
            var memberName = paramTypes.Length > 0 
                ? $"M:{typeName}.{method.Name}({paramTypes})"
                : $"M:{typeName}.{method.Name}";

            var memberElement = doc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            if (memberElement == null)
            {
                // 尝试不带参数的匹配（有时 XML 文档格式略有不同）
                memberElement = doc.Descendants("member")
                    .FirstOrDefault(m => m.Attribute("name")?.Value?.StartsWith($"M:{typeName}.{method.Name}") == true);
            }

            var summary = memberElement?.Element("summary")?.Value?.Trim();
            return summary;
        }

        /// <summary>
        /// 从 XML 文档中获取类的 summary 注释
        /// </summary>
        private static string GetTypeSummary(Type type)
        {
            var doc = GetXmlDoc(type.Assembly);
            if (doc == null) return null;

            var typeName = type.FullName.Replace("+", ".");
            var memberName = $"T:{typeName}";

            var memberElement = doc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            return memberElement?.Element("summary")?.Value?.Trim();
        }

        /// <summary>
        /// 获取类型的 XML 文档格式名称
        /// </summary>
        private static string GetXmlTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetXmlTypeName));
                var baseName = genericDef.FullName.Substring(0, genericDef.FullName.IndexOf('`'));
                return $"{baseName}{{{genericArgs}}}";
            }
            
            if (type.IsArray)
            {
                return GetXmlTypeName(type.GetElementType()) + "[]";
            }

            return type.FullName ?? type.Name;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 1. Remove the generic _MethodDispatch route
            var keysToRemove = swaggerDoc.Paths.Keys.Where(k => k.Contains("{_verb}")).ToList();
            foreach (var key in keysToRemove)
            {
                swaggerDoc.Paths.Remove(key);
            }

            // 2. Scan all JmController subclasses from all loaded assemblies
            var scannedAssemblies = new HashSet<Assembly>();
            var controllerTypes = new List<Type>();
            
            // Scan the Puff framework assembly
            var puffAssembly = Assembly.GetAssembly(typeof(JmController));
            if (puffAssembly != null)
            {
                scannedAssemblies.Add(puffAssembly);
                controllerTypes.AddRange(GetJmControllerTypes(puffAssembly));
            }

            // Scan the entry assembly
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && !scannedAssemblies.Contains(entryAssembly))
            {
                scannedAssemblies.Add(entryAssembly);
                controllerTypes.AddRange(GetJmControllerTypes(entryAssembly));
            }

            // Scan all referenced assemblies from the entry assembly
            if (entryAssembly != null)
            {
                foreach (var refAssemblyName in entryAssembly.GetReferencedAssemblies())
                {
                    try
                    {
                        var refAssembly = Assembly.Load(refAssemblyName);
                        if (refAssembly != null && !scannedAssemblies.Contains(refAssembly))
                        {
                            scannedAssemblies.Add(refAssembly);
                            controllerTypes.AddRange(GetJmControllerTypes(refAssembly));
                        }
                    }
                    catch
                    {
                        // Ignore assemblies that cannot be loaded
                    }
                }
            }

            foreach (var controllerType in controllerTypes)
            {
                ProcessController(swaggerDoc, context, controllerType);
            }

            // 3. 简化框架类型的 Schema（只保留类型名称和描述）
            SimplifyFrameworkSchemas(swaggerDoc);
        }

        /// <summary>
        /// 从程序集中获取所有 JmController 子类
        /// </summary>
        private IEnumerable<Type> GetJmControllerTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(JmController)) && !t.IsAbstract);
            }
            catch
            {
                // Ignore type loading errors
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// 简化框架类型的 Schema - 只保留类型名称和描述，移除深度反射的字段
        /// </summary>
        private void SimplifyFrameworkSchemas(OpenApiDocument swaggerDoc)
        {
            if (swaggerDoc.Components?.Schemas == null) return;

            // 需要简化的框架类型名称
            var frameworkTypeNames = new HashSet<string>
            {
                "HttpRequest", "HttpResponse", "HttpContext", 
                "IceApiRequest", "IceApiResponse"
            };

            // 需要完全移除的系统类型（这些是框架类型的子字段，不需要在 Swagger 中显示）
            var schemasToRemove = swaggerDoc.Components.Schemas.Keys
                .Where(key => 
                    // 保留框架类型本身（会被简化）
                    !frameworkTypeNames.Contains(key) &&
                    // 移除系统类型
                    (key.Contains("KeyValuePair")
                    || key.StartsWith("System")
                    || key.StartsWith("Microsoft")
                    || key.StartsWith("ConnectionInfo")
                    || key.StartsWith("WebSocketManager")
                    || key.StartsWith("ClaimsPrincipal")
                    || key.StartsWith("ClaimsIdentity")
                    || key.StartsWith("Stream")
                    || key.StartsWith("Assembly")
                    || key.StartsWith("Type")
                    || key.StartsWith("MethodInfo")
                    || key.StartsWith("PropertyInfo")
                    || key.StartsWith("FieldInfo")
                    || key.StartsWith("MemberInfo")
                    || key.StartsWith("ParameterInfo")
                    || key.StartsWith("Module")
                    || key.StartsWith("Oid")
                    || key.StartsWith("IPAddress")
                    || key.StartsWith("X509")
                    || key.StartsWith("X500")
                    || key.StartsWith("PublicKey")
                    || key.StartsWith("AsymmetricAlgorithm")
                    || key.Contains("Handle")
                    || key.Contains("Attributes")
                    || key.Contains("Span")
                    || key.Contains("Memory")))
                .ToList();

            // 移除不需要的 schema
            foreach (var key in schemasToRemove)
            {
                swaggerDoc.Components.Schemas.Remove(key);
            }

            // 简化框架类型 schema
            foreach (var typeName in frameworkTypeNames)
            {
                if (swaggerDoc.Components.Schemas.ContainsKey(typeName))
                {
                    swaggerDoc.Components.Schemas[typeName] = CreateSimplifiedFrameworkSchema(typeName);
                }
            }
        }

        /// <summary>
        /// 为框架类型创建简化的 Schema（只有类型名称和描述）
        /// </summary>
        private OpenApiSchema CreateSimplifiedFrameworkSchema(string typeName)
        {
            var descriptions = new Dictionary<string, string>
            {
                { "HttpRequest", "ASP.NET Core HTTP 请求对象（由框架自动注入）" },
                { "HttpContext", "ASP.NET Core HTTP 上下文对象（由框架自动注入）" },
                { "HttpResponse", "ASP.NET Core HTTP 响应对象" },
                { "IceApiRequest", "Puff 框架请求对象（包含 Headers、Cookies、Query、Body Stream 等）" },
                { "IceApiResponse", "Puff 框架响应对象（可返回 JSON、文本、二进制数据或文件下载）" },
            };

            return new OpenApiSchema
            {
                Type = "object",
                Title = typeName,
                Description = descriptions.TryGetValue(typeName, out var desc) ? desc : $"{typeName} (框架类型)",
                // 不定义 Properties，避免显示字段
            };
        }

        private void ProcessController(OpenApiDocument swaggerDoc, DocumentFilterContext context, Type controllerType)
        {
            // Get the route prefix (e.g., "My" from [Route("My")])
            // Default to controller name without "Controller" suffix if no attribute
            string routePrefix = controllerType.Name.Replace("Controller", "");
            var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();
            if (routeAttr != null)
            {
                routePrefix = routeAttr.Template.Replace("[controller]", controllerType.Name.Replace("Controller", ""));
            }

            // Retrieve JmModule using JmGlobal (internal access)
            var jmModule = JmGlobal.Retrieve(controllerType);
            if (jmModule == null) return;

            // Access private "Map" field via reflection
            var mapField = typeof(JmModule).GetField("Map", BindingFlags.NonPublic | BindingFlags.Instance);
            var map = mapField?.GetValue(jmModule) as Dictionary<string, JmMethod>;

            if (map == null) return;

            // 添加 Controller 的 Tag 描述（从 XML 注释获取）
            var controllerSummary = GetTypeSummary(controllerType);
            if (!string.IsNullOrEmpty(controllerSummary))
            {
                // 确保 Tags 列表存在
                if (swaggerDoc.Tags == null)
                {
                    swaggerDoc.Tags = new List<OpenApiTag>();
                }
                
                // 检查是否已存在该 Tag
                var existingTag = swaggerDoc.Tags.FirstOrDefault(t => t.Name == routePrefix);
                if (existingTag == null)
                {
                    swaggerDoc.Tags.Add(new OpenApiTag
                    {
                        Name = routePrefix,
                        Description = controllerSummary
                    });
                }
                else if (string.IsNullOrEmpty(existingTag.Description))
                {
                    existingTag.Description = controllerSummary;
                }
            }

            foreach (var kvp in map)
            {
                var jmMethod = kvp.Value;
                GeneratePathItem(swaggerDoc, context, routePrefix, jmMethod);
            }
        }

        private void GeneratePathItem(OpenApiDocument swaggerDoc, DocumentFilterContext context, string routePrefix, JmMethod jmMethod)
        {
            var path = $"/{routePrefix}/{jmMethod.Name}";
            
            // 从 XML 文档获取方法注释
            var methodSummary = GetMethodSummary(jmMethod.MI);
            
            // 检测方法上的 HTTP 方法属性
            var httpMethods = GetHttpMethods(jmMethod.MI);
            
            // 如果没有显式指定 HTTP 方法，默认同时支持 GET 和 POST
            if (httpMethods.Count == 0)
            {
                httpMethods.Add(OperationType.Get);
                httpMethods.Add(OperationType.Post);
            }

            var pathItem = swaggerDoc.Paths.ContainsKey(path) 
                ? swaggerDoc.Paths[path] 
                : new OpenApiPathItem();

            // 为每个 HTTP 方法生成操作
            foreach (var httpMethod in httpMethods)
            {
                var operation = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = routePrefix } },
                    OperationId = $"{routePrefix}_{jmMethod.Name}_{httpMethod.ToString().ToLowerInvariant()}",
                    Summary = methodSummary,  // 方法的 <summary> 注释
                    Responses = new OpenApiResponses()
                };

                // Request Body
                if (jmMethod.Attr.Flags == IceApiFlag.Http)
                {
                    // Http mode - assumes raw request handling
                    if (string.IsNullOrEmpty(operation.Summary))
                    {
                        operation.Summary = "Raw HTTP Handler";
                    }
                }
                else
                {
                    // JSON mode (default) or JsonIn
                    // GET 请求通常不使用 RequestBody，但 Puff 框架支持通过 query string 传递参数
                    if (httpMethod == OperationType.Get)
                    {
                        // 对于 GET 请求，将参数作为 query parameters 处理
                        GenerateQueryParameters(operation, context, jmMethod);
                    }
                    else
                    {
                        // POST 等其他方法使用 RequestBody
                        GenerateRequestBody(operation, context, jmMethod);
                    }
                }

                // Response
                GenerateResponse(operation, context, jmMethod);

                pathItem.AddOperation(httpMethod, operation);
            }

            if (!swaggerDoc.Paths.ContainsKey(path))
            {
                swaggerDoc.Paths.Add(path, pathItem);
            }
        }

        /// <summary>
        /// 检测方法上的 HTTP 方法属性（HttpGet, HttpPost, HttpPut, HttpDelete 等）
        /// </summary>
        private List<OperationType> GetHttpMethods(MethodInfo method)
        {
            var httpMethods = new List<OperationType>();

            // 检查各种 HTTP 方法属性
            if (method.GetCustomAttribute<HttpGetAttribute>() != null)
                httpMethods.Add(OperationType.Get);
            
            if (method.GetCustomAttribute<HttpPostAttribute>() != null)
                httpMethods.Add(OperationType.Post);
            
            if (method.GetCustomAttribute<HttpPutAttribute>() != null)
                httpMethods.Add(OperationType.Put);
            
            if (method.GetCustomAttribute<HttpDeleteAttribute>() != null)
                httpMethods.Add(OperationType.Delete);
            
            if (method.GetCustomAttribute<HttpPatchAttribute>() != null)
                httpMethods.Add(OperationType.Patch);
            
            if (method.GetCustomAttribute<HttpHeadAttribute>() != null)
                httpMethods.Add(OperationType.Head);
            
            if (method.GetCustomAttribute<HttpOptionsAttribute>() != null)
                httpMethods.Add(OperationType.Options);

            return httpMethods;
        }

        /// <summary>
        /// 为 GET 请求生成 query parameters（从方法参数生成）
        /// </summary>
        private void GenerateQueryParameters(OpenApiOperation operation, DocumentFilterContext context, JmMethod jmMethod)
        {
            var parameters = jmMethod.MI.GetParameters();
            if (parameters.Length == 0) return;

            // 过滤掉框架自动注入的类型（这些不是用户传入的参数）
            var businessParams = parameters
                .Where(p => !NoReflectTypes.Contains(p.ParameterType) 
                         && !IsFrameworkType(p.ParameterType))
                .ToArray();

            if (businessParams.Length == 0) return;

            if (operation.Parameters == null)
            {
                operation.Parameters = new List<OpenApiParameter>();
            }

            foreach (var param in businessParams)
            {
                // 只支持简单类型作为 query parameter
                if (IsSimpleType(param.ParameterType))
                {
                    var paramSchema = context.SchemaGenerator.GenerateSchema(param.ParameterType, context.SchemaRepository);
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = param.Name,
                        In = ParameterLocation.Query,
                        Required = !IsNullable(param),
                        Schema = paramSchema,
                        Description = GetParameterDescription(param)
                    });
                }
            }
        }

        /// <summary>
        /// 判断参数是否可为 null
        /// </summary>
        private bool IsNullable(ParameterInfo param)
        {
            if (param.ParameterType.IsValueType)
            {
                return Nullable.GetUnderlyingType(param.ParameterType) != null;
            }
            return true; // 引用类型默认可为 null
        }

        /// <summary>
        /// 获取参数的描述（从 XML 注释）
        /// </summary>
        private string GetParameterDescription(ParameterInfo param)
        {
            // 可以扩展从 XML 注释中获取参数描述
            // 这里先返回 null，后续可以完善
            return null;
        }

        private void GenerateRequestBody(OpenApiOperation operation, DocumentFilterContext context, JmMethod jmMethod)
        {
            var parameters = jmMethod.MI.GetParameters();
            if (parameters.Length == 0) return;

            // IceApiFlag.Http 模式使用 IceApiRequest 参数，不需要生成请求体 schema
            if (jmMethod.Attr.Flags == IceApiFlag.Http)
            {
                return;
            }

            // 过滤掉框架自动注入的类型（这些不是用户传入的参数）
            var businessParams = parameters
                .Where(p => !NoReflectTypes.Contains(p.ParameterType) 
                         && !IsFrameworkType(p.ParameterType))
                .ToArray();

            if (businessParams.Length == 0) return;

            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            foreach (var param in businessParams)
            {
                // 简单类型和业务类型可以正常生成 schema
                if (IsSimpleType(param.ParameterType) || IsBusinessType(param.ParameterType))
                {
                    var paramSchema = context.SchemaGenerator.GenerateSchema(param.ParameterType, context.SchemaRepository);
                    schema.Properties.Add(param.Name, paramSchema);
                }
                else
                {
                    // 对于复杂的未知类型，使用 object 类型避免过度反射
                    schema.Properties.Add(param.Name, new OpenApiSchema { Type = "object" });
                }
            }

            if (schema.Properties.Count == 0) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }

        /// <summary>
        /// 判断是否为框架类型（不应该出现在 Swagger 文档中）
        /// </summary>
        private bool IsFrameworkType(Type type)
        {
            if (type == null) return false;
            
            var ns = type.Namespace;
            if (string.IsNullOrEmpty(ns)) return false;

            // 排除 ASP.NET Core、System、Microsoft 命名空间下的复杂类型
            return ns.StartsWith("Microsoft.AspNetCore") 
                || ns.StartsWith("System.IO") 
                || ns.StartsWith("System.Net")
                || ns.StartsWith("System.Security")
                || ns.StartsWith("System.Reflection")
                || ns.StartsWith("System.Threading")
                || ns.StartsWith("System.Runtime");
        }

        /// <summary>
        /// 判断是否为简单类型（可以安全地生成 schema）
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            if (type == null) return false;
            
            // 处理 Nullable<T>
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                type = underlyingType;

            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type.IsEnum;
        }

        /// <summary>
        /// 判断是否为业务类型（用户定义的类型，可以反射生成 schema）
        /// </summary>
        private bool IsBusinessType(Type type)
        {
            if (type == null) return false;
            
            var ns = type.Namespace;
            if (string.IsNullOrEmpty(ns)) return true; // 没有命名空间的类型通常是用户定义的

            // 排除系统和框架命名空间
            if (ns.StartsWith("System") || ns.StartsWith("Microsoft"))
                return false;

            // 允许集合类型（但需要检查元素类型）
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) 
                    || genericDef == typeof(IEnumerable<>) || genericDef == typeof(Dictionary<,>))
                {
                    // 检查泛型参数是否为业务类型或简单类型
                    return type.GetGenericArguments().All(t => IsSimpleType(t) || IsBusinessType(t));
                }
            }

            return true;
        }

        private void GenerateResponse(OpenApiOperation operation, DocumentFilterContext context, JmMethod jmMethod)
        {
            var returnType = jmMethod.MI.ReturnType;
            var flags = jmMethod.Attr.Flags;
            OpenApiSchema schema;

            if (flags == IceApiFlag.Http || flags == IceApiFlag.JsonIn)
            {
                // Http/JsonIn 模式：返回 IceApiResponse，引用简化的 schema
                schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = "IceApiResponse"
                    }
                };
                
                // 确保 IceApiResponse schema 存在于文档中
                EnsureFrameworkSchemaExists(context, "IceApiResponse");
            }
            else // IceApiFlag.Json (默认)
            {
                // Json 模式：标准 JSON 返回，可以正常生成 schema
                schema = GenerateJsonResponseSchema(context, jmMethod, returnType);
            }

            operation.Responses.Add("200", new OpenApiResponse
            {
                Description = "Success",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            });
        }

        /// <summary>
        /// 确保框架类型 schema 存在于 SchemaRepository 中
        /// </summary>
        private void EnsureFrameworkSchemaExists(DocumentFilterContext context, string typeName)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(typeName))
            {
                context.SchemaRepository.Schemas.Add(typeName, CreateSimplifiedFrameworkSchema(typeName));
            }
        }

        /// <summary>
        /// 为 IceApiFlag.Json 模式生成标准 JSON 响应 schema
        /// </summary>
        private OpenApiSchema GenerateJsonResponseSchema(DocumentFilterContext context, JmMethod jmMethod, Type returnType)
        {
            var retNames = jmMethod.Rets;
            var schema = new OpenApiSchema();

            // void 返回类型
            if (returnType == typeof(void))
            {
                return new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["stat"] = new OpenApiSchema { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("ok") }
                    }
                };
            }

            // Tuple 返回类型，使用 Ret 属性指定的名称
            if (retNames != null && retNames.Length > 0 && IsTuple(returnType))
            {
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>();

                var genericArgs = returnType.GetGenericArguments();
                for (int i = 0; i < Math.Min(genericArgs.Length, retNames.Length); i++)
                {
                    if (IsSimpleType(genericArgs[i]) || IsBusinessType(genericArgs[i]))
                    {
                        var propSchema = context.SchemaGenerator.GenerateSchema(genericArgs[i], context.SchemaRepository);
                        schema.Properties.Add(retNames[i], propSchema);
                    }
                    else
                    {
                        schema.Properties.Add(retNames[i], new OpenApiSchema { Type = "object" });
                    }
                }
                
                // 添加 stat 字段
                schema.Properties["stat"] = new OpenApiSchema { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("ok") };
            }
            else if (retNames != null && retNames.Length == 1)
            {
                // 单个返回值，使用 Ret 属性指定的名称包装
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>();

                if (IsSimpleType(returnType) || IsBusinessType(returnType))
                {
                    var propSchema = context.SchemaGenerator.GenerateSchema(returnType, context.SchemaRepository);
                    schema.Properties[retNames[0]] = propSchema;
                }
                else
                {
                    schema.Properties[retNames[0]] = new OpenApiSchema { Type = "object" };
                }
                
                schema.Properties["stat"] = new OpenApiSchema { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("ok") };
            }
            else
            {
                // 无 Ret 属性，返回对象直接作为响应
                if (IsSimpleType(returnType) || IsBusinessType(returnType))
                {
                    schema = context.SchemaGenerator.GenerateSchema(returnType, context.SchemaRepository);
                }
                else
                {
                    schema = new OpenApiSchema { Type = "object" };
                }
            }

            return schema;
        }

        private bool IsTuple(Type type)
        {
             if (!type.IsGenericType) return false;
             var openType = type.GetGenericTypeDefinition();
             return openType.FullName.StartsWith("System.ValueTuple");
        }
    }
}
