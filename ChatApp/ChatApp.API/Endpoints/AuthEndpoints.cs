using ChatApp.API.Services;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IdentityRegisterDto = ChatApp.API.DTOs.Identity.RegisterDto;
using IdentityLoginDto = ChatApp.API.DTOs.Identity.LoginDto;
using IdentityRefreshTokenDto = ChatApp.API.DTOs.Identity.RefreshTokenDto;
using ApiRegisterDto = ChatApp.API.DTOs.RegisterDto;
using ApiLoginDto = ChatApp.API.DTOs.LoginDto;

namespace ChatApp.API.Endpoints;

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

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithOpenApi();

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization()
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] ApiRegisterDto registerDto,
        IIdentityServiceClient identityClient)
    {
        var result = await identityClient.RegisterAsync(new IdentityRegisterDto(
            registerDto.Email,
            registerDto.Password,
            registerDto.UserName ?? "User",
            registerDto.UserName ?? "User"
        ));

        if (result == null)
        {
            return Results.BadRequest(new { message = "Registration failed" });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] ApiLoginDto loginDto,
        IIdentityServiceClient identityClient)
    {
        var result = await identityClient.LoginAsync(new IdentityLoginDto(
            loginDto.Email,
            loginDto.Password
        ));

        if (result == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] IdentityRefreshTokenDto refreshDto,
        IIdentityServiceClient identityClient)
    {
        var result = await identityClient.RefreshTokenAsync(new IdentityRefreshTokenDto(
            refreshDto.RefreshToken
        ));

        if (result == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal user,
        IIdentityServiceClient identityClient)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Extract token from Authorization header
        var token = user.FindFirst("access_token")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Results.Unauthorized();
        }

        var identityUser = await identityClient.GetUserByIdAsync(userId, token);
        if (identityUser == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(identityUser);
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal user,
        [FromBody] IdentityRefreshTokenDto refreshDto,
        IIdentityServiceClient identityClient)
    {
        var token = user.FindFirst("access_token")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return Results.Unauthorized();
        }

        var success = await identityClient.RevokeTokenAsync(refreshDto.RefreshToken, token);

        if (!success)
        {
            return Results.BadRequest(new { message = "Logout failed" });
        }

        return Results.Ok(new { message = "Logout successful" });
    }
}