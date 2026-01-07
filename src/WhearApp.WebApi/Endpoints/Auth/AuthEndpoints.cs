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
            .WithApiMetadata(
                "Register user",
                "Creates a new user account.")
            .Produces<ApiResponse<LoginResponse>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}