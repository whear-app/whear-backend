using FluentResults;
using Microsoft.AspNetCore.Identity;
using WhearApp.Application.Common;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Application.Identity.Repositories;
using WhearApp.Core.Identity;

namespace WhearApp.Application.Identity.Services;

public class AuthService(
    UserManager<UserEntity> userManager,
    IIdentityRepository identityRepository,
    IUnitOfWork unitOfWork,
    IJwtService jwtService)
    : IAuthService
{
    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return Errors.Validation("Passwords do not match");

        var existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
            return Errors.Conflict("Username already exists");

        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            return Errors.Conflict("Email already exists");

        var user = new UserEntity()
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(x => x.Code, x => x.Description);
            return Errors.Validation(errors);
        }

        await userManager.AddToRoleAsync(user, "User");

        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        // TODO: Send email confirmation link

        // Return success without LoginResponse (registration doesn't auto-login)
        return Result.Ok<LoginResponse>(null!)
            .WithSuccess("Registration successful. Please check your email to confirm your account.");
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress)
    {
        // Find user
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null)
            return Errors.Unauthorized("Invalid username or password");

        if (await userManager.IsLockedOutAsync(user))
            return Errors.Forbidden("Account is locked out. Please try again later.");

        if (!user.EmailConfirmed && userManager.Options.SignIn.RequireConfirmedEmail)
            return Errors.Unauthorized("Email not confirmed. Please confirm your email first.");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
                return Errors.Forbidden("Account is locked out due to multiple failed login attempts.");

            return Errors.Unauthorized("Invalid username or password");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);

        // Generate JWT token
        var accessToken = jwtService.GenerateToken(
            user.Id.ToString(),
            user.UserName!,
            roles.ToList());

        // Generate refresh token
        var refreshTokenValue = jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        };
        await identityRepository.AddRefreshTokenAsync(refreshToken, user.Id);

        var userInfo = new UserInfo(
            user.Id,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.TwoFactorEnabled);

        var loginResponse = new LoginResponse(
            accessToken,
            refreshTokenValue,
            "Bearer",
            3600,
            userInfo);

        return Result.Ok(loginResponse);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await identityRepository.GetRefreshTokenAsync(refreshToken);

        if (token is not { IsActive: true })
            return Errors.Unauthorized("Invalid or expired refresh token");

        var user = token.User;

        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = jwtService.GenerateToken(
            user.Id.ToString(),
            user.UserName!,
            roles.ToList());

        var newRefreshToken = jwtService.GenerateRefreshToken();

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken;
        await unitOfWork.SaveChangesAsync();

        // Create new refresh token
        var refreshTokenEntity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        };

        await identityRepository.AddRefreshTokenAsync(refreshTokenEntity, user.Id);
        var userInfo = new UserInfo(
            user.Id,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.TwoFactorEnabled);

        var loginResponse = new LoginResponse(
            newAccessToken,
            newRefreshToken,
            "Bearer",
            3600,
            userInfo);

        return Result.Ok(loginResponse);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await identityRepository.GetRefreshTokenAsync(refreshToken);

        if (token is not { IsActive: true })
            return Errors.NotFound("Invalid refresh token");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        await unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result<UserInfo>> GetUserInfoAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Errors.NotFound("User not found");
        }
        
        var userInfo = new UserInfo(
            user.Id,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.TwoFactorEnabled);
        
        return Result.Ok(userInfo);
    }
}