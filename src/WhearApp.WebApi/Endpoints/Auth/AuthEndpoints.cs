using Microsoft.AspNetCore.Mvc;
using WhearApp.Application.Common;
using WhearApp.Application.Identity;
using WhearApp.WebApi.Extensions;

namespace WhearApp.WebApi.Endpoints.Auth;

public static partial class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", Register)
            .WithApiMetadata("Register user", "Creates a new user account.")
            .WithStandardResponses<LoginResponse>();
        
        group.MapPost("/login", Login)
            .RequireRateLimiting("login_fixed")
            .WithApiMetadata("User login", "Authenticates a user and returns JWT and refresh tokens.")
            .WithStandardResponses<LoginResponse>();

        group.MapPost("/refresh-token", RefreshToken)
            .WithApiMetadata("Refresh access token", "Generates a new access token using a valid refresh token.")
            .WithStandardResponses<LoginResponse>();

        group.MapPost("/revoke-token", RevokeToken)
            .RequireAuthorization()
            .WithApiMetadata("Revoke refresh token", "Revokes a refresh token to prevent its future use.")
            .WithStandardResponses();

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithApiMetadata("User logout", "Revokes all active refresh tokens for the current user.")
            .Produces<ApiResponse>()
            .WithStandardResponses();

    }
}