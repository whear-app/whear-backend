using WhearApp.WebApi.Endpoints.Auth;
using WhearApp.WebApi.Endpoints.System;

namespace WhearApp.WebApi.Endpoints;

public static class RegisterV1EndpointExtensions
{
    public static void MapV1Endpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/health")
            .WithTags("System")
            .MapHealthEndpoints();
        
        endpoints.MapGroup("/auth")
            .WithTags("Auth")
            .MapAuthEndpoints();

    }
}