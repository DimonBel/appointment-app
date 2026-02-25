using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints.Identity;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterDto model,
        IAuthService authService)
    {
        var (success, message, response) = await authService.RegisterAsync(model);
        
        if (!success || response == null)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginDto model,
        IAuthService authService)
    {
        var (success, message, response) = await authService.LoginAsync(model);
        
        if (!success || response == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(response);
    }
}
