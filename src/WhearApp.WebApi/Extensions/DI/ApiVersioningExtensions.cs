using Microsoft.OpenApi.Models;
using WhearApp.Application.Common;

namespace WhearApp.WebApi.Extensions.DI;

/// <summary>
///     Extension methods for configuring API versioning
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    ///     Configure OpenAPI/Swagger for versioned APIs
    /// </summary>
    public static void ConfigureOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "WhearApp API",
                    Version = "v1",
                    Description = "API for managing WhearApp functionalities",
                    Contact = new OpenApiContact
                    {
                        Name = "WhearApp Support",
                        Email = "thtntrungnam@gmail.com"
                    }
                };
                return Task.CompletedTask;
            });
            
            // Transform schema support generic types
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;

                // Handle ApiResponse<T> generic type
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    var dataType = type.GetGenericArguments()[0];
                    
                    // Custom schema ID for Orval to recognize as generic
                    // Format: ApiResponse_T where T is the actual type name
                    var schemaId = $"ApiResponseOf{dataType.Name}";
                    
                    // Ensure data property has correct reference
                    if (schema.Properties.TryGetValue("data", out var dataProperty))
                    {
                        // Add description for better documentation
                        dataProperty.Description = $"The response data of type {dataType.Name}";
                    }

                    // Mark as nullable where appropriate
                    if (schema.Properties.TryGetValue("data", out var arg1Property))
                    {
                        arg1Property.Nullable = true;
                    }
                    if (schema.Properties.TryGetValue("message", out var property))
                    {
                        property.Nullable = true;
                    }
                }

                // Handle base ApiResponse (non-generic)
                if (type == typeof(ApiResponse))
                {
                    if (schema.Properties.TryGetValue("message", out var property))
                    {
                        property.Nullable = true;
                    }
                }

                return Task.CompletedTask;
            });

            // Add operation transformers for better documentation
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                // Add common response headers
                foreach (var response in operation.Responses.Values)
                {
                    response.Headers ??= new Dictionary<string, OpenApiHeader>();
                    
                    if (!response.Headers.ContainsKey("X-Request-ID"))
                    {
                        response.Headers["X-Request-ID"] = new OpenApiHeader
                        {
                            Description = "Request identifier for tracking",
                            Schema = new OpenApiSchema { Type = "string" }
                        };
                    }
                }

                return Task.CompletedTask;
            });
        });
    }
}