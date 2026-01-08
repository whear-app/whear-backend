using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WhearApp.Application.Identity;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.WebApi.Extensions;

namespace WhearApp.WebApi.Endpoints.Account;

public static partial class AccountEndpoints
{
    private static async Task<IResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] IAccountService accountService,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await accountService.ChangePasswordAsync(userId, request);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IAccountService accountService)
    {
        var result = await accountService.ForgotPasswordAsync(request);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IAccountService accountService)
    {
        var result = await accountService.ResetPasswordAsync(request);
        return result.ToHttpResult();

    }

    private static async Task<IResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] IAccountService accountService)
    {
        var result = await accountService.ConfirmEmailAsync(request);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ResendEmailConfirmation(
        [FromBody] ResendEmailConfirmationRequest request,
        [FromServices] IAccountService accountService)
    {
        var result = await accountService.ResendEmailConfirmationAsync(request);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetCurrentUser(
        [FromServices] IAuthService authService,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var userInfo = await authService.GetUserInfoAsync(userId);
        return userInfo.ToHttpResult();
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] IAccountService accountService,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await accountService.UpdateProfileAsync(userId, request);
        return result.ToHttpResult();
    }
}