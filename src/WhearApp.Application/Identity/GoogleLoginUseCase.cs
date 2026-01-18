using Microsoft.AspNetCore.Identity;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Application.Identity.Dto;
using WhearApp.Core.Identity;

namespace WhearApp.Application.Identity;
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

public class GoogleLoginUseCase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly UserManager<UserEntity> _userManager;

    public GoogleLoginUseCase(IGoogleAuthService googleAuthService, IJwtService jwtService, UserManager<UserEntity> userManager)
    {
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _userManager = userManager;
    }

    public async Task<AuthResponse> ExecuteAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        var tokenResponse = await _googleAuthService.ExchangeCodeAsync(
            request.AuthorizationCode,
            request.CodeVerifier,
            ct);
        
        var userInfo = await _googleAuthService.VerifyIdTokenAsync(
            tokenResponse.IdToken,
            ct);
        
        var user = await GetOrCreateUserAsync(userInfo, ct);
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateToken(
            user.Id.ToString(),
            user.UserName!,
            roles.ToList());
        
        var refreshToken = _jwtService.GenerateRefreshToken();
        
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
        var user = await _userManager.FindByEmailAsync(userInfo.Email);
        if (user != null)
            return user;

        user = new UserEntity
        {
            UserName = userInfo.Email,
            Email = userInfo.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(x => x.Code, x => x.Description);
            throw new Exception("Failed to create user: " + string.Join(", ", errors.Values));
        }

        await _userManager.AddToRoleAsync(user, "User");

        return user;
    }
}