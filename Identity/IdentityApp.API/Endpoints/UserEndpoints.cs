using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using IdentityApp.Domain.Entity;
using System.Security.Claims;

namespace IdentityApp.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/{userId:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithOpenApi()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/email/{email}", GetUserByEmailAsync)
            .WithName("GetUserByEmail")
            .WithOpenApi()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/username/{username}", GetUserByUsernameAsync)
            .WithName("GetUserByUsername")
            .WithOpenApi()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAllUsersAsync)
            .WithName("GetAllUsers")
            .WithOpenApi()
            .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK);

        group.MapGet("/search", SearchUsersAsync)
            .WithName("SearchUsers")
            .WithOpenApi()
            .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK);

        group.MapPut("/{userId:guid}", UpdateUserAsync)
            .WithName("UpdateUser")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{userId:guid}", DeleteUserAsync)
            .WithName("DeleteUser")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{userId:guid}/online-status", SetUserOnlineStatusAsync)
            .WithName("SetUserOnlineStatus")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{userId:guid}/avatar", UploadAvatarAsync)
            .WithName("UploadUserAvatar")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetUserByIdAsync(
        Guid userId,
        IUserService userService)
    {
        var user = await userService.GetUserByIdAsync(userId);
        return user == null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> GetUserByEmailAsync(
        string email,
        IUserService userService)
    {
        var user = await userService.GetUserByEmailAsync(email);
        return user == null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> GetUserByUsernameAsync(
        string username,
        IUserService userService)
    {
        var user = await userService.GetUserByUsernameAsync(username);
        return user == null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> GetAllUsersAsync(IUserService userService)
    {
        var users = await userService.GetAllUsersAsync();
        return Results.Ok(users);
    }

    private static async Task<IResult> SearchUsersAsync(
        [FromQuery] string? query,
        IUserService userService)
    {
        var users = await userService.SearchUsersAsync(query ?? string.Empty);
        return Results.Ok(users);
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid userId,
        [FromBody] UserDto userDto,
        IUserService userService)
    {
        var result = await userService.UpdateUserAsync(userId, userDto);
        return result ? Results.Ok() : Results.BadRequest();
    }

    private static async Task<IResult> DeleteUserAsync(
        Guid userId,
        IUserService userService)
    {
        var result = await userService.DeleteUserAsync(userId);
        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> SetUserOnlineStatusAsync(
        Guid userId,
        [FromBody] bool isOnline,
        IUserService userService)
    {
        var result = await userService.SetUserOnlineStatusAsync(userId, isOnline);
        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> UploadAvatarAsync(
        Guid userId,
        HttpContext context,
        [FromForm] IFormFile avatar,
        UserManager<AppIdentityUser> userManager,
        IAvatarStorageService avatarStorageService)
    {
        if (avatar is null || avatar.Length == 0)
        {
            return Results.BadRequest(new { message = "Avatar file is required." });
        }

        var requesterId = TryGetUserId(context.User);
        var isAdmin = context.User.IsInRole("Admin");
        if (!isAdmin && requesterId != userId)
        {
            return Results.Forbid();
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(avatar.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Only JPG, PNG, WEBP and GIF files are allowed." });
        }

        const long maxAvatarSize = 5 * 1024 * 1024;
        if (avatar.Length > maxAvatarSize)
        {
            return Results.BadRequest(new { message = "Avatar file size must be less than 5MB." });
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Results.NotFound(new { message = "User not found." });
        }

        await using var stream = avatar.OpenReadStream();
        var avatarUrl = await avatarStorageService.UploadAvatarAsync(
            stream,
            avatar.Length,
            avatar.FileName,
            avatar.ContentType,
            user.Email ?? user.UserName ?? userId.ToString());

        user.AvatarUrl = avatarUrl;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Results.BadRequest(new { message = errors });
        }

        return Results.Ok(new { avatarUrl });
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
