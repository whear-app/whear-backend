using WhearApp.WebApi.Endpoints.Account;
using WhearApp.WebApi.Endpoints.Auth;

namespace WhearApp.WebApi.Endpoints;

public static class RegisterV1EndpointExtensions
{
    public static void MapV1Endpoints(this IEndpointRouteBuilder endpoints)
    {
        
        endpoints.MapGroup("/auth")
            .WithTags("Auth")
            .MapAuthEndpoints();
        
        endpoints.MapGroup("/account")
            .WithTags("Account")
            .MapAccountEndpoints();
        
        endpoints.MapGroup("/oauth2")
            .WithTags("OAuth2 Providers")
            .MapOAuth2ProvidersEndpoints();

    }
}