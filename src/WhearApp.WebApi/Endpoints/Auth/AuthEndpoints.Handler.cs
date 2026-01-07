using Microsoft.AspNetCore.Mvc;
using WhearApp.Application.Identity;
using WhearApp.Application.Identity.Services;
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
}