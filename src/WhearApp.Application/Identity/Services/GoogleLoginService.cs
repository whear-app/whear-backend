using Microsoft.AspNetCore.Identity;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Application.Identity.Dto;
using WhearApp.Core.Identity;

namespace WhearApp.Application.Identity.Services;
public class GoogleLoginRequest
{
    public string AuthorizationCode { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}

public class GoogleLoginService(
    IGoogleAuthService googleAuthService,
    IJwtService jwtService,
    UserManager<UserEntity> userManager)
{
    public async Task<AuthResponse> ExecuteAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        var tokenResponse = await googleAuthService.ExchangeCodeAsync(
            request.AuthorizationCode,
            request.CodeVerifier,
            ct);
        
        var userInfo = await googleAuthService.VerifyIdTokenAsync(
            tokenResponse.IdToken,
            ct);
        
        var user = await GetOrCreateUserAsync(userInfo, ct);
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateToken(
            user.Id.ToString(),
            user.UserName!,
            roles.ToList());
        
        var refreshToken = jwtService.GenerateRefreshToken();
        
        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresIn = 3600,
            RefreshToken = refreshToken
        };
    }

    private async Task<UserEntity> GetOrCreateUserAsync(
        GoogleUserInfo userInfo,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(userInfo.Email);
        if (user != null)
            return user;

        user = new UserEntity
        {
            UserName = userInfo.Email,
            Email = userInfo.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(x => x.Code, x => x.Description);
            throw new Exception("Failed to create user: " + string.Join(", ", errors.Values));
        }

        await userManager.AddToRoleAsync(user, "User");

        return user;
    }
}