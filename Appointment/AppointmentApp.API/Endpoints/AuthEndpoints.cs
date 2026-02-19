using Identity.API.DTOs;
using AppointmentApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/login", LoginAsync)
            .WithName("LoginUser")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces<IdentityUserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterDto model,
        IIdentityServiceClient identityService)
    {
        var result = await identityService.RegisterAsync(model);

        if (result == null)
        {
            return Results.BadRequest(new { message = "Registration failed. Please check your input." });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginDto model,
        IIdentityServiceClient identityService)
    {
        var result = await identityService.LoginAsync(model);

        if (result == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenDto model,
        IIdentityServiceClient identityService)
    {
        var result = await identityService.RefreshTokenAsync(model);

        if (result == null)
        {
            return Results.BadRequest(new { message = "Token refresh failed" });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        HttpContext httpContext,
        IIdentityServiceClient identityService)
    {
        // Get user ID from JWT claims
        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Get access token from authorization header
        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring("Bearer ".Length);
        var user = await identityService.GetUserByIdAsync(userId, token);

        if (user == null)
        {
            return Results.NotFound(new { message = "User not found" });
        }

        return Results.Ok(user);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IIdentityServiceClient identityService)
    {
        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                await identityService.RevokeTokenAsync(userId, token);
            }
        }

        return Results.Ok(new { message = "Logged out successfully" });
    }
}
