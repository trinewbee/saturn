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

            // 2. Scan all JmController subclasses
            var controllerTypes = Assembly.GetAssembly(typeof(JmController))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(JmController)) && !t.IsAbstract);

            // Also check the entry assembly for controllers (e.g. the app using the framework)
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && entryAssembly != Assembly.GetAssembly(typeof(JmController)))
            {
                var appControllers = entryAssembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(JmController)) && !t.IsAbstract);
                controllerTypes = controllerTypes.Concat(appControllers);
            }

            foreach (var controllerType in controllerTypes)
            {
                ProcessController(swaggerDoc, context, controllerType);
            }

            // 3. 简化框架类型的 Schema（只保留类型名称和描述）
            SimplifyFrameworkSchemas(swaggerDoc);
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
            
            var operation = new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = routePrefix } },
                OperationId = $"{routePrefix}_{jmMethod.Name}",
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
                GenerateRequestBody(operation, context, jmMethod);
            }

            // Response
            GenerateResponse(operation, context, jmMethod);

            var pathItem = new OpenApiPathItem();
            
            // Determine HTTP verb. Puff generally uses POST for RPC style, but supports GET if specified?
            // JmController._MethodDispatch has [HttpGet] and [HttpPost].
            // Let's assume POST for all IceApis as they are typically RPC, but we can check attributes if needed.
            // For now, mapping to POST as that's the primary channel for JSON args.
            pathItem.AddOperation(OperationType.Post, operation);
            
            // Also map GET if it makes sense? Puff usually does POST for JSON args.
            // If args are simple, maybe GET works via query string parsing in Puff?
            // JmWebInvoker.ParseQueryMap parses query string.
            // So GET is possible. Let's add both or just POST?
            // The design doc example shows POST. Let's stick to POST for now to avoid clutter, 
            // or add GET if it's a query-like method?
            // Safe bet: POST.

            if (swaggerDoc.Paths.ContainsKey(path))
            {
                // Merge operations if path exists (rare for RPC but possible)
                var existing = swaggerDoc.Paths[path];
                existing.AddOperation(OperationType.Post, operation);
            }
            else
            {
                swaggerDoc.Paths.Add(path, pathItem);
            }
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
