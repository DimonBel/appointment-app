using ChatApp.API.DTOs;
using ChatApp.Domain.Entity;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ChatApp.API.Endpoints;

public static class FriendshipEndpoints
{
    public static void MapFriendshipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/friends")
            .RequireAuthorization()
            .WithTags("Friends");

        group.MapPost("/request", SendFriendRequestAsync)
            .WithName("SendFriendRequest");

        group.MapPost("/{id}/accept", AcceptFriendRequestAsync)
            .WithName("AcceptFriendRequest");

        group.MapPost("/{id}/decline", DeclineFriendRequestAsync)
            .WithName("DeclineFriendRequest");

        group.MapGet("/", GetFriendsAsync)
            .WithName("GetFriends");

        group.MapGet("/requests/pending", GetPendingRequestsAsync)
            .WithName("GetPendingFriendRequests");

        group.MapGet("/requests/sent", GetSentRequestsAsync)
            .WithName("GetSentFriendRequests");

        group.MapGet("/status/{userId}", GetFriendshipStatusAsync)
            .WithName("GetFriendshipStatus");

        group.MapDelete("/{id}", RemoveFriendAsync)
            .WithName("RemoveFriend");

        group.MapGet("/ids", GetFriendIdsAsync)
            .WithName("GetFriendIds");
    }

    private static async Task<IResult> SendFriendRequestAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        [FromBody] SendFriendRequestDto dto)
    {
        var logger = loggerFactory.CreateLogger("FriendshipEndpoints");
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        try
        {
            var friendship = await friendshipService.SendFriendRequestAsync(currentUserId.Value, dto.AddresseeId);

            // Send notification to the addressee via Notification service
            _ = Task.Run(async () =>
            {
                try
                {
                    var requester = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var requesterName = requester?.UserName ?? "Someone";

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    var eventPayload = JsonSerializer.Serialize(new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestSent",
                        payload = JsonSerializer.Serialize(new
                        {
                            receiverId = dto.AddresseeId,
                            senderId = currentUserId.Value,
                            senderName = requesterName,
                            friendshipId = friendship.Id
                        })
                    });
                    var content = new StringContent(eventPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("/api/notifications/events", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Failed to dispatch FriendRequestSent event. StatusCode: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to dispatch FriendRequestSent notification event");
                }
            });

            return Results.Ok(new { friendship.Id, friendship.Status, message = "Friend request sent successfully." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AcceptFriendRequestAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        Guid id)
    {
        var logger = loggerFactory.CreateLogger("FriendshipEndpoints");
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        try
        {
            var friendship = await friendshipService.AcceptFriendRequestAsync(id, currentUserId.Value);

            // Notify the requester
            _ = Task.Run(async () =>
            {
                try
                {
                    var accepter = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var accepterName = accepter?.UserName ?? "Someone";

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    var eventPayload = JsonSerializer.Serialize(new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestAccepted",
                        payload = JsonSerializer.Serialize(new
                        {
                            requesterId = friendship.RequesterId,
                            accepterName,
                            friendshipId = friendship.Id
                        })
                    });
                    var content = new StringContent(eventPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("/api/notifications/events", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Failed to dispatch FriendRequestAccepted event. StatusCode: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to dispatch FriendRequestAccepted notification event");
                }
            });

            return Results.Ok(new { friendship.Id, friendship.Status, message = "Friend request accepted." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeclineFriendRequestAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        Guid id)
    {
        var logger = loggerFactory.CreateLogger("FriendshipEndpoints");
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        try
        {
            var friendship = await friendshipService.DeclineFriendRequestAsync(id, currentUserId.Value);

            // Notify the requester
            _ = Task.Run(async () =>
            {
                try
                {
                    var decliner = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var declinerName = decliner?.UserName ?? "Someone";

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    var eventPayload = JsonSerializer.Serialize(new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestDeclined",
                        payload = JsonSerializer.Serialize(new
                        {
                            requesterId = friendship.RequesterId,
                            declinerName
                        })
                    });
                    var content = new StringContent(eventPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("/api/notifications/events", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Failed to dispatch FriendRequestDeclined event. StatusCode: {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to dispatch FriendRequestDeclined notification event");
                }
            });

            return Results.Ok(new { friendship.Id, friendship.Status, message = "Friend request declined." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetFriendsAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        var friendships = await friendshipService.GetFriendsAsync(currentUserId.Value);
        var dtos = new List<FriendshipDto>();

        foreach (var f in friendships)
        {
            var requester = await chatService.GetUserByIdAsync(f.RequesterId);
            var addressee = await chatService.GetUserByIdAsync(f.AddresseeId);
            dtos.Add(new FriendshipDto(
                f.Id, f.RequesterId, f.AddresseeId,
                requester?.UserName ?? "Unknown", addressee?.UserName ?? "Unknown",
                requester?.Email ?? "", addressee?.Email ?? "",
                requester?.AvatarUrl, addressee?.AvatarUrl,
                f.Status.ToString(), f.CreatedAt, f.UpdatedAt));
        }

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetPendingRequestsAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        var requests = await friendshipService.GetPendingRequestsAsync(currentUserId.Value);
        var dtos = new List<FriendshipDto>();

        foreach (var f in requests)
        {
            var requester = await chatService.GetUserByIdAsync(f.RequesterId);
            var addressee = await chatService.GetUserByIdAsync(f.AddresseeId);
            dtos.Add(new FriendshipDto(
                f.Id, f.RequesterId, f.AddresseeId,
                requester?.UserName ?? "Unknown", addressee?.UserName ?? "Unknown",
                requester?.Email ?? "", addressee?.Email ?? "",
                requester?.AvatarUrl, addressee?.AvatarUrl,
                f.Status.ToString(), f.CreatedAt, f.UpdatedAt));
        }

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetSentRequestsAsync(
        IFriendshipService friendshipService,
        IChatService chatService,
        HttpContext httpContext)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        var requests = await friendshipService.GetSentRequestsAsync(currentUserId.Value);
        var dtos = new List<FriendshipDto>();

        foreach (var f in requests)
        {
            var requester = await chatService.GetUserByIdAsync(f.RequesterId);
            var addressee = await chatService.GetUserByIdAsync(f.AddresseeId);
            dtos.Add(new FriendshipDto(
                f.Id, f.RequesterId, f.AddresseeId,
                requester?.UserName ?? "Unknown", addressee?.UserName ?? "Unknown",
                requester?.Email ?? "", addressee?.Email ?? "",
                requester?.AvatarUrl, addressee?.AvatarUrl,
                f.Status.ToString(), f.CreatedAt, f.UpdatedAt));
        }

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetFriendshipStatusAsync(
        IFriendshipService friendshipService,
        HttpContext httpContext,
        Guid userId)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        var friendship = await friendshipService.GetFriendshipBetweenAsync(currentUserId.Value, userId);

        if (friendship == null)
            return Results.Ok(new FriendStatusDto(userId, "none", null));

        string status;
        if (friendship.Status == Domain.Enums.FriendshipStatus.Accepted)
            status = "friends";
        else if (friendship.Status == Domain.Enums.FriendshipStatus.Pending && friendship.RequesterId == currentUserId.Value)
            status = "pending_sent";
        else if (friendship.Status == Domain.Enums.FriendshipStatus.Pending && friendship.AddresseeId == currentUserId.Value)
            status = "pending_received";
        else
            status = "none";

        return Results.Ok(new FriendStatusDto(userId, status, friendship.Id));
    }

    private static async Task<IResult> RemoveFriendAsync(
        IFriendshipService friendshipService,
        HttpContext httpContext,
        Guid id)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        try
        {
            await friendshipService.RemoveFriendAsync(id, currentUserId.Value);
            return Results.Ok(new { message = "Friend removed." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetFriendIdsAsync(
        IFriendshipService friendshipService,
        HttpContext httpContext)
    {
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        var friendIds = await friendshipService.GetFriendIdsAsync(currentUserId.Value);
        return Results.Ok(friendIds);
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
