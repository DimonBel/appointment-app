using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        // User management endpoints
        group.MapGet("/users", GetAllUsersAsync)
            .WithName("AdminGetAllUsers")
            .WithOpenApi()
            .Produces<IEnumerable<AdminUserDto>>(StatusCodes.Status200OK);

        group.MapGet("/statistics", GetUserStatisticsAsync)
            .WithName("AdminGetUserStatistics")
            .WithOpenApi()
            .Produces<UserStatisticsDto>(StatusCodes.Status200OK);

        group.MapPost("/users", CreateUserAsync)
            .WithName("AdminCreateUser")
            .WithOpenApi()
            .Produces<UserDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/users/{userId:guid}", UpdateUserAsync)
            .WithName("AdminUpdateUser")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/users/{userId:guid}", DeleteUserAsync)
            .WithName("AdminDeleteUser")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/users/{userId:guid}/toggle-status", ToggleUserActiveStatusAsync)
            .WithName("AdminToggleUserStatus")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/users/{userId:guid}/roles", AssignRoleAsync)
            .WithName("AdminAssignRole")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/users/{userId:guid}/roles/{roleName}", RemoveRoleAsync)
            .WithName("AdminRemoveRole")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/users/{userId:guid}/reset-password", ResetUserPasswordAsync)
            .WithName("AdminResetPassword")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllUsersAsync(IAdminService adminService)
    {
        var users = await adminService.GetAllUsersWithDetailsAsync();
        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserStatisticsAsync(IAdminService adminService)
    {
        var stats = await adminService.GetUserStatisticsAsync();
        return Results.Ok(stats);
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateUserDto createUserDto,
        IAdminService adminService)
    {
        var user = await adminService.CreateUserAsync(createUserDto);
        return user == null ? Results.BadRequest() : Results.Created($"/api/users/{user.Id}", user);
    }

    private static async Task<IResult> ToggleUserActiveStatusAsync(
        Guid userId,
        IAdminService adminService)
    {
        var result = await adminService.ToggleUserActiveStatusAsync(userId);
        return result ? Results.Ok(new { message = "User status updated successfully" }) : Results.NotFound();
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid userId,
        [FromBody] UserDto userDto,
        IAdminService adminService)
    {
        var result = await adminService.UpdateUserAsync(userId, userDto);
        return result ? Results.Ok(new { message = "User updated successfully" }) : Results.BadRequest();
    }

    private static async Task<IResult> DeleteUserAsync(
        Guid userId,
        IAdminService adminService)
    {
        var result = await adminService.DeleteUserAsync(userId);
        return result ? Results.Ok(new { message = "User deleted successfully" }) : Results.NotFound();
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid userId,
        [FromBody] string roleName,
        IAdminService adminService)
    {
        var result = await adminService.AssignRoleAsync(userId, roleName);
        return result ? Results.Ok(new { message = "Role assigned successfully" }) : Results.BadRequest();
    }

    private static async Task<IResult> RemoveRoleAsync(
        Guid userId,
        string roleName,
        IAdminService adminService)
    {
        var result = await adminService.RemoveRoleAsync(userId, roleName);
        return result ? Results.Ok(new { message = "Role removed successfully" }) : Results.NotFound();
    }

    private static async Task<IResult> ResetUserPasswordAsync(
        Guid userId,
        [FromBody] string newPassword,
        IAdminService adminService)
    {
        var result = await adminService.ResetUserPasswordAsync(userId, newPassword);
        return result ? Results.Ok(new { message = "Password reset successfully" }) : Results.BadRequest();
    }
}