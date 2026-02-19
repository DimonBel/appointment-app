using Identity.API.DTOs;
using Identity.Domain.Interfaces;
using Identity.Domain.Entity;
using Identity.Service.Services;
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

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/current", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterDto model,
        IAuthService authService)
    {
        var (success, message, user) = await authService.RegisterAsync(model.Email, model.Password, model.UserName, model.FirstName, model.LastName);
        
        if (!success)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(new { message, user });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginDto model,
        IAuthService authService)
    {
        var (success, message, user) = await authService.LoginAsync(model.Email, model.Password, model.RememberMe);
        
        if (!success)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new { message, user });
    }

    private static async Task<IResult> LogoutAsync(IAuthService authService)
    {
        await authService.LogoutAsync();
        return Results.Ok(new { message = "Logout successful" });
    }

    private static async Task<IResult> GetCurrentUserAsync(IAuthService authService)
    {
        var user = await authService.GetCurrentUserAsync();
        
        if (user == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(user);
    }
}
