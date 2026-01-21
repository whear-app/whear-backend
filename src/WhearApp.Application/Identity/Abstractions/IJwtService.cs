using System.Security.Claims;

namespace WhearApp.Application.Identity.Abstractions;

public interface IJwtService
{
    string GenerateToken(string userId, string username, List<string> roles, List<Claim>? customClaims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}