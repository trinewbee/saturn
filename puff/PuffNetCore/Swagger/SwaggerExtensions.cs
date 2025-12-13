using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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
