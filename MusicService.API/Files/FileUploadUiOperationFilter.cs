using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MusicService.API.Files
{
    public sealed class FileUploadUiOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionName = context.MethodInfo.Name;
            if (string.Equals(actionName, "Upload", StringComparison.Ordinal))
            {
                operation.RequestBody = BuildBody(new Dictionary<string, OpenApiSchema>
                {
                    ["file"] = new OpenApiSchema { Type = "string", Format = "binary" }
                }, new[] { "file" });
                return;
            }

            if (string.Equals(actionName, "UploadMultiple", StringComparison.Ordinal))
            {
                operation.RequestBody = BuildBody(new Dictionary<string, OpenApiSchema>
                {
                    ["files"] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "string", Format = "binary" }
                    }
                }, new[] { "files" });
                return;
            }

            if (string.Equals(actionName, "UploadChunked", StringComparison.Ordinal) ||
                context.ApiDescription.RelativePath?.EndsWith("files/upload/chunked", StringComparison.OrdinalIgnoreCase) == true)
            {
                operation.Parameters.Clear();
                operation.RequestBody = BuildBody(new Dictionary<string, OpenApiSchema>
                {
                    ["file"] = new OpenApiSchema { Type = "string", Format = "binary" }
                }, new[] { "file" });
                operation.RequestBody.Required = true;
            }

            if (string.Equals(actionName, "Stream", StringComparison.Ordinal) ||
                context.ApiDescription.RelativePath?.EndsWith("files/{id}/stream", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!operation.Responses.TryGetValue("200", out var response))
                {
                    response = new OpenApiResponse { Description = "file stream" };
                    operation.Responses["200"] = response;
                }

                response.Content["application/octet-stream"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = "string", Format = "binary" }
                };
            }
        }

        private static OpenApiRequestBody BuildBody(IDictionary<string, OpenApiSchema> properties, IEnumerable<string> required)
        {
            return new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = new HashSet<string>(required)
                        }
                    }
                }
            };
        }
    }
}
