using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
}
