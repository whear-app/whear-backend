using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WhearApp.Application.Common;
using WhearApp.Application.Identity;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Infrastructure.Database;
using WhearApp.WebApi.Extensions;

namespace WhearApp.WebApi.Endpoints.Auth;

public static partial class AuthEndpoints
{
    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthService authService)
    {
        var result = await authService.RegisterAsync(request);
        return result.ToHttpResult();        
    }
    
    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.LoginAsync(request, ipAddress);
        return result.ToHttpResult();        
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IAuthService authService,
        HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        return result.ToHttpResult();        
    }

    private static async Task<IResult> RevokeToken(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IAuthService authService,
        HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authService.RevokeTokenAsync(request.RefreshToken, ipAddress);

        return result.ToHttpResult();        

    }

    private static async Task<IResult> Logout(
        [FromServices] IAuthService authService,
        [FromServices] ApplicationDbContext dbContext,
        ClaimsPrincipal user,
        HttpContext httpContext)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        // Revoke all active refresh tokens for this user
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var activeTokens = dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToList();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await dbContext.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok("Logged out successfully"));
    }
}