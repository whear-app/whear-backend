using FluentResults;

namespace WhearApp.Application.Identity.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<Result<UserInfo>> GetUserInfoAsync(Guid userId);
}
