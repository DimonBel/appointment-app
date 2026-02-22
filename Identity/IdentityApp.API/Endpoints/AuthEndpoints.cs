using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using IdentityApp.Domain.Entity;
using Microsoft.AspNetCore.Mvc;
using IdentityApp.API.DTOs;

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

        group.MapPost("/register-with-avatar", RegisterWithAvatarAsync)
            .WithName("RegisterWithAvatar")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi()
            .Produces<AuthResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/confirm-email", ConfirmEmailAsync)
            .WithName("ConfirmEmail")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

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

        group.MapPost("/change-password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // Role management
        var roleGroup = app.MapGroup("/api/roles")
            .WithTags("Roles")
            .RequireAuthorization("AdminOnly");

        roleGroup.MapGet("/", GetAllRolesAsync)
            .WithName("GetAllRoles")
            .WithOpenApi();

        roleGroup.MapPost("/assign", AssignRoleAsync)
            .WithName("AssignRole")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        roleGroup.MapPost("/remove", RemoveRoleAsync)
            .WithName("RemoveRole")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        roleGroup.MapGet("/user/{userId:guid}", GetUserRolesAsync)
            .WithName("GetUserRoles")
            .WithOpenApi();
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

    private static async Task<IResult> RegisterWithAvatarAsync(
        [FromForm] RegisterWithAvatarDto model,
        IAuthService authService,
        IAvatarStorageService avatarStorageService)
    {
        if (model.Avatar is { Length: > 0 })
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(model.Avatar.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Only JPG, PNG, WEBP and GIF files are allowed for avatar." });
            }

            const long maxAvatarSize = 5 * 1024 * 1024;
            if (model.Avatar.Length > maxAvatarSize)
            {
                return Results.BadRequest(new { message = "Avatar file size must be less than 5MB." });
            }
        }

        string? avatarUrl = null;
        if (model.Avatar is { Length: > 0 })
        {
            await using var stream = model.Avatar.OpenReadStream();
            avatarUrl = await avatarStorageService.UploadAvatarAsync(
                stream,
                model.Avatar.Length,
                model.Avatar.FileName,
                model.Avatar.ContentType,
                model.Email);
        }

        var registerDto = new RegisterDto
        {
            Email = model.Email,
            Password = model.Password,
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            Role = model.Role,
            AvatarUrl = avatarUrl
        };

        var (success, message, response) = await authService.RegisterAsync(registerDto);

        if (!success)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(new { message, response, avatarUrl });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginDto model,
        IAuthService authService)
    {
        var (success, message, response) = await authService.LoginAsync(model);

        if (!success)
        {
            return Results.Json(new { message }, statusCode: StatusCodes.Status401Unauthorized);
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

    private static async Task<IResult> ConfirmEmailAsync(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        IAuthService authService)
    {
        var (success, message, response) = await authService.ConfirmEmailAsync(userId, token);
        return success
            ? Results.Ok(new { message, response })
            : Results.BadRequest(new { message });
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

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordDto model,
        UserManager<AppIdentityUser> userManager,
        IHttpClientFactory httpClientFactory,
        HttpContext context)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Results.BadRequest(new { message = errors });
        }

        // Fire password changed notification (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("NotificationService");
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userId = Guid.Parse(userId),
                    userName = user.UserName ?? user.Email ?? "User",
                    email = user.Email
                });
                await client.PostAsJsonAsync("/api/notifications/events", new
                {
                    sourceService = "IdentityService",
                    eventName = "PasswordChanged",
                    payload
                });
            }
            catch { /* non-critical */ }
        });

        return Results.Ok(new { message = "Password changed successfully" });
    }

    private static async Task<IResult> GetAllRolesAsync(
        RoleManager<AppIdentityRole> roleManager)
    {
        var roles = roleManager.Roles.Select(r => new { r.Id, r.Name, r.Description }).ToList();
        return Results.Ok(roles);
    }

    private static async Task<IResult> AssignRoleAsync(
        [FromBody] RoleAssignmentDto model,
        UserManager<AppIdentityUser> userManager,
        RoleManager<AppIdentityRole> roleManager)
    {
        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var roleExists = await roleManager.RoleExistsAsync(model.RoleName);
        if (!roleExists)
            return Results.BadRequest(new { message = $"Role '{model.RoleName}' does not exist" });

        var isInRole = await userManager.IsInRoleAsync(user, model.RoleName);
        if (isInRole)
            return Results.BadRequest(new { message = $"User already has role '{model.RoleName}'" });

        var result = await userManager.AddToRoleAsync(user, model.RoleName);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Results.BadRequest(new { message = errors });
        }

        return Results.Ok(new { message = $"Role '{model.RoleName}' assigned successfully" });
    }

    private static async Task<IResult> RemoveRoleAsync(
        [FromBody] RoleAssignmentDto model,
        UserManager<AppIdentityUser> userManager)
    {
        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var result = await userManager.RemoveFromRoleAsync(user, model.RoleName);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Results.BadRequest(new { message = errors });
        }

        return Results.Ok(new { message = $"Role '{model.RoleName}' removed successfully" });
    }

    private static async Task<IResult> GetUserRolesAsync(
        Guid userId,
        UserManager<AppIdentityUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var roles = await userManager.GetRolesAsync(user);
        return Results.Ok(new { userId, roles });
    }
}
