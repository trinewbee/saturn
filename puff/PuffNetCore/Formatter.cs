using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Nano.Json;
using Nano.Nuts;

namespace Puff.NetCore
{
    public class JmOutputFormatter : OutputFormatter
    {
        public JmOutputFormatter()
        {
            foreach (var name in new string[] { "application/json", "text/plain" })
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(name));
        }

        protected override bool CanWriteType(Type type) => type == typeof(IceApiResponse);

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            var apir = context.Object as IceApiResponse;
            if (apir == null)
                return;

            response.StatusCode = apir.HttpStatusCode;
            if (apir.Cookies != null && apir.Cookies.Count != 0)
            {
                foreach (var pair in apir.Cookies)
                    response.Cookies.Append(pair.Key, pair.Value);
            }

            if (apir.Headers != null)
            {
                foreach (var pair in apir.Headers)
                    response.Headers.Add(pair.Key, pair.Value);
            }

            if (apir.Json != null)
            {
                var text = DObject.ImportJson(apir.Json).ToString();
                var data = Encoding.UTF8.GetBytes(text);
                response.ContentType = IceApiResponse.CT_JsonUtf8;
                response.ContentLength = data.Length;
                await response.Body.WriteAsync(data, 0, data.Length);
            }
            else if (apir.Text != null)
            {
                response.ContentType = apir.ContentType != null ? apir.ContentType : IceApiResponse.CT_TextUtf8;
                var data = Encoding.UTF8.GetBytes(apir.Text);
                response.ContentLength = data.Length;
                await response.Body.WriteAsync(data, 0, data.Length);
            }
            else if (apir.Data != null)
            {
                response.ContentType = apir.ContentType != null ? apir.ContentType : IceApiResponse.CT_Binary;
                response.ContentLength = apir.Data.Length;
                await response.Body.WriteAsync(apir.Data, 0, apir.Data.Length);
            }
            else
            {
                System.Diagnostics.Debug.Fail("UnsupportedIceApiResponse");
                throw new NutsException("UnsupportedIceApiResponse", "UnsupportedIceApiResponse");
            }
        }
    }
}
