using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Application.Identity.Dto;

namespace WhearApp.Infrastructure.Identity.Services;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleAuthService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["Google:ClientId"] 
            ?? throw new InvalidOperationException("Google:ClientId not configured");
        _clientSecret = configuration["Google:ClientSecret"] 
            ?? throw new InvalidOperationException("Google:ClientSecret not configured");
        _redirectUri = configuration["Google:RedirectUri"] 
            ?? throw new InvalidOperationException("Google:RedirectUri not configured");
    }

    public async Task<GoogleTokenResponse> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken ct = default)
    {
        const string tokenEndpoint = "https://oauth2.googleapis.com/token";

        var requestBody = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };

        var response = await _httpClient.PostAsync(
            tokenEndpoint,
            new FormUrlEncodedContent(requestBody),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Failed to exchange authorization code: {response.StatusCode} - {error}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponseJson>(ct)
            ?? throw new InvalidOperationException("Invalid token response");

        return new GoogleTokenResponse
        {
            AccessToken = tokenResponse.access_token,
            IdToken = tokenResponse.id_token,
            ExpiresIn = tokenResponse.expires_in,
            TokenType = tokenResponse.token_type,
            RefreshToken = tokenResponse.refresh_token
        };
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(
        string idToken,
        CancellationToken ct = default)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            if (!payload.EmailVerified)
            {
                throw new InvalidOperationException("Email not verified");
            }

            return new GoogleUserInfo
            {
                Sub = payload.Subject,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified,
                Name = payload.Name,
                Picture = payload.Picture,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName
            };
        }
        catch (InvalidJwtException ex)
        {
            throw new InvalidOperationException("Invalid ID token", ex);
        }
    }

    // JSON response model
    private class GoogleTokenResponseJson
    {
        public string access_token { get; set; } = string.Empty;
        public string id_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string token_type { get; set; } = string.Empty;
        public string? refresh_token { get; set; }
    }
}