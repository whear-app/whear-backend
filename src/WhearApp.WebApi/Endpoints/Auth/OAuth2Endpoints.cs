using WhearApp.Application.Identity;
using WhearApp.WebApi.Extensions;

namespace WhearApp.WebApi.Endpoints.Auth;

public static partial class OAuth2Endpoints
{
    public static void MapOAuth2ProvidersEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/google/login", GoogleLogin)
            .WithApiMetadata("Google Login", "Authenticates a user using Google OAuth2 and returns JWT and refresh tokens.")
            .WithStandardResponses<LoginResponse>();

        group.MapGet("/google/config", GetGoogleConfig)
            .WithApiMetadata("Get Google OAuth2 Config", "Retrieves the Google OAuth2 configuration for client-side use.")
            .WithStandardResponses<GoogleOAuthConfig>();
    }
    
}