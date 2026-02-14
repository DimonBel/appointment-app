using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/revoke/{userId}", RevokeTokenAsync)
            .WithName("RevokeToken")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/validate", ValidateTokenAsync)
            .WithName("ValidateToken")
            .WithOpenApi()
            .Produces<bool>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterDto model,
        IAuthService authService)
    {
        var (success, message, response) = await authService.RegisterAsync(model);

        if (!success)
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

        if (!success)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenDto model,
        IAuthService authService)
    {
        var (success, message, response) = await authService.RefreshTokenAsync(model);

        if (!success)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> RevokeTokenAsync(
        string userId,
        IAuthService authService)
    {
        var result = await authService.RevokeTokenAsync(userId);

        if (!result)
        {
            return Results.BadRequest(new { message = "Failed to revoke token" });
        }

        return Results.Ok(new { message = "Token revoked successfully" });
    }

    private static async Task<IResult> ValidateTokenAsync(
        [FromBody] string token,
        IAuthService authService)
    {
        var isValid = await authService.ValidateTokenAsync(token);
        return Results.Ok(new { isValid });
    }
}
