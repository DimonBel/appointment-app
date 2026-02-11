using ChatApp.API.DTOs;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.API.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .RequireAuthorization()
            .WithTags("Chat");

        group.MapGet("/users", GetAllUsersAsync)
            .WithName("GetAllUsers")
            .WithOpenApi();

        group.MapGet("/users/search", SearchUsersAsync)
            .WithName("SearchUsers")
            .WithOpenApi();

        group.MapGet("/users/{id}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithOpenApi();

        group.MapGet("/messages/{userId}", GetMessagesBetweenUsersAsync)
            .WithName("GetMessagesBetweenUsers")
            .WithOpenApi();

        group.MapPost("/messages", SendMessageAsync)
            .WithName("SendMessage")
            .WithOpenApi();

        group.MapGet("/messages/recent", GetRecentMessagesAsync)
            .WithName("GetRecentMessages")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAllUsersAsync(
        IChatService chatService,
        HttpContext httpContext)
    {
        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
        {
            return Results.Unauthorized();
        }

        var users = await chatService.GetAllUsersAsync();
        // Filter out the current user from the list
        var filteredUsers = users.Where(u => u.Id != currentUserIdGuid);
        return Results.Ok(filteredUsers);
    }

    private static async Task<IResult> GetUserByIdAsync(
        IChatService chatService,
        Guid id)
    {
        var user = await chatService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(user);
    }

    private static async Task<IResult> GetMessagesBetweenUsersAsync(
        IChatService chatService,
        HttpContext httpContext,
        Guid userId,
        int page = 1,
        int pageSize = 50)
    {
        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
        {
            return Results.Unauthorized();
        }

        var messages = await chatService.GetMessagesBetweenUsersAsync(currentUserIdGuid, userId, page, pageSize);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SendMessageAsync(
        IChatService chatService,
        HttpContext httpContext,
        [FromBody] SendMessageDto model)
    {
        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
        {
            return Results.Unauthorized();
        }

        try
        {
            var message = await chatService.SendMessageAsync(currentUserIdGuid, model.ReceiverId, model.Content);
            return Results.Ok(message);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error sending message: {ex}");
            return Results.StatusCode(500); // Or a more appropriate error response
        }
    }

    private static async Task<IResult> GetRecentMessagesAsync(
        IChatService chatService,
        HttpContext httpContext,
        int count = 20)
    {
        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
        {
            return Results.Unauthorized();
        }

        var messages = await chatService.GetUserRecentMessagesAsync(currentUserIdGuid, count);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SearchUsersAsync(
        IChatService chatService,
        HttpContext httpContext,
        string? query = null)
    {
        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var currentUserIdGuid))
        {
            return Results.Unauthorized();
        }

        var users = await chatService.SearchUsersAsync(query ?? string.Empty);
        // Filter out the current user from search results
        var filteredUsers = users.Where(u => u.Id != currentUserIdGuid);
        return Results.Ok(filteredUsers);
    }
}