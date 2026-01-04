using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using Puff.NetCore;
using Puff.Marshal;

namespace Puff.NetCore.Swagger
{
    public static class SwaggerExtensions
    {
        public static void AddPuffSwagger(this IServiceCollection services, string title = "Puff API", string version = "v1")
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });

                // Register the Puff document filter
                c.DocumentFilter<PuffDocumentFilter>();
                // 添加这一行：使用类的全名(FullName)作为 Schema ID
                c.CustomSchemaIds(type => type.FullName);
                // Exclude JmController subclasses and [IceApi] methods from automatic scanning
                // PuffDocumentFilter will manually add them based on [IceApi] attributes
                // This prevents "Ambiguous HTTP method" errors for methods without [Http*] attributes
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (apiDesc.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        // 排除 JmController 的子类
                        if (controllerActionDescriptor.ControllerTypeInfo.IsSubclassOf(typeof(JmController)))
                        {
                            return false;
                        }
                        
                        // 排除带有 [IceApi] 属性的方法（这些由 PuffDocumentFilter 手动处理）
                        var methodInfo = controllerActionDescriptor.MethodInfo;
                        if (methodInfo.GetCustomAttribute<IceApiAttribute>() != null)
                        {
                            return false;
                        }
                    }
                    return true;
                });

                // Resolve conflicts for actions without explicit HTTP method binding
                // This is needed because Puff uses _MethodDispatch for routing, 
                // but Swashbuckle's API explorer may find public methods on JmController subclasses
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                // Include XML comments if available
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var xmlFile = $"{entryAssembly.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
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
}
