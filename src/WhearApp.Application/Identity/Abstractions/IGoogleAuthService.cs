using WhearApp.Application.Identity.Dto;

namespace WhearApp.Application.Identity.Abstractions;

public interface IGoogleAuthService
{
    Task<GoogleTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default);
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}