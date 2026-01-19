using Microsoft.AspNetCore.Mvc;
using WhearApp.Application.Identity.Services;

namespace WhearApp.WebApi.Endpoints.Auth;

public static partial class OAuth2Endpoints
{
    private static async Task<IResult> GoogleLogin([FromBody] GoogleLoginRequest request, [FromServices] GoogleLoginService googleLoginUseCase, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AuthorizationCode) ||
            string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            return Results.BadRequest("AuthorizationCode and CodeVerifier are required.");
        }
        var response = await googleLoginUseCase.ExecuteAsync(request, ct);
        return Results.Ok(response);
    }

    private static IResult GetGoogleConfig([FromServices] IConfiguration configuration)
    {
        var config = new GoogleOAuthConfig
        {
            ClientId = configuration["Google:ClientId"] ?? string.Empty,
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            RedirectUri = configuration["Google:RedirectUri"] ?? string.Empty,
            Scope = "openid email profile"
        };
        return Results.Ok(config);
    }
}
public class GoogleOAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
