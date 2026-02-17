using ChatApp.API.DTOs;
using ChatApp.API.Hubs;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        IFriendshipService friendshipService,
        HttpContext httpContext)
    {
        var currentUserIdGuid = TryGetUserId(httpContext.User);

        if (!currentUserIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var friendIds = (await friendshipService.GetFriendIdsAsync(currentUserIdGuid.Value)).ToHashSet();
        if (friendIds.Count == 0)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var users = await chatService.GetAllUsersAsync();
        var filteredUsers = users.Where(u => friendIds.Contains(u.Id));
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
        var currentUserIdGuid = TryGetUserId(httpContext.User);

        if (!currentUserIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var messages = await chatService.GetMessagesBetweenUsersAsync(currentUserIdGuid.Value, userId, page, pageSize);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SendMessageAsync(
        IChatService chatService,
        IFriendshipService friendshipService,
        IHubContext<ChatHub> hubContext,
        HttpContext httpContext,
        [FromBody] SendMessageDto model)
    {
        var currentUserIdGuid = TryGetUserId(httpContext.User);

        if (!currentUserIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        // Check friendship before allowing message
        var areFriends = await friendshipService.AreFriendsAsync(currentUserIdGuid.Value, model.ReceiverId);
        if (!areFriends)
        {
            return Results.BadRequest(new { error = "You can only send messages to friends. Send a friend request first." });
        }

        try
        {
            var message = await chatService.SendMessageAsync(currentUserIdGuid.Value, model.ReceiverId, model.Content);

            await hubContext.Clients.Group(model.ReceiverId.ToString()).SendAsync(
                "ReceiveMessage",
                message.SenderId.ToString(),
                message.Content,
                message.Id.ToString(),
                message.CreatedAt.ToString("o")
            );

            await hubContext.Clients.Group(currentUserIdGuid.Value.ToString()).SendAsync(
                "MessageSent",
                message.ReceiverId.ToString(),
                message.Content,
                message.Id.ToString(),
                message.CreatedAt.ToString("o")
            );

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
        var currentUserIdGuid = TryGetUserId(httpContext.User);

        if (!currentUserIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var messages = await chatService.GetUserRecentMessagesAsync(currentUserIdGuid.Value, count);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SearchUsersAsync(
        IChatService chatService,
        HttpContext httpContext,
        string? query = null)
    {
        var currentUserIdGuid = TryGetUserId(httpContext.User);

        if (!currentUserIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var users = await chatService.SearchUsersAsync(query ?? string.Empty);
        // Filter out the current user from search results
        var filteredUsers = users.Where(u => u.Id != currentUserIdGuid.Value);
        return Results.Ok(filteredUsers);
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}