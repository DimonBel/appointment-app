using ChatApp.API.DTOs;
using ChatApp.API.DTOs.Identity;
using ChatApp.API.Services;
using ChatApp.Domain.Entity;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        IIdentityServiceClient identityServiceClient,
        HttpContext httpContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        [FromBody] SendFriendRequestDto dto)
    {
        var logger = loggerFactory.CreateLogger("FriendshipEndpoints");
        var currentUserId = TryGetUserId(httpContext.User);
        if (!currentUserId.HasValue) return Results.Unauthorized();

        // Extract access token from Authorization header
        var accessToken = ExtractAccessToken(httpContext);
        if (string.IsNullOrEmpty(accessToken))
        {
            return Results.Unauthorized();
        }

        // Validate roles: Only clients (User/Patient) can add doctors (Doctor/Professional) as friends
        try
        {
            var requesterRoles = await identityServiceClient.GetUserRolesAsync(currentUserId.Value.ToString(), accessToken);
            var addresseeRoles = await identityServiceClient.GetUserRolesAsync(dto.AddresseeId.ToString(), accessToken);

            if (requesterRoles == null || addresseeRoles == null)
            {
                return Results.BadRequest(new { error = "Unable to verify user roles." });
            }

            var requesterRolesList = requesterRoles.ToList();
            var addresseeRolesList = addresseeRoles.ToList();

            // Define valid client roles and doctor roles
            var clientRoles = new[] { "User", "Patient" };
            var doctorRoles = new[] { "Doctor", "Professional" };

            // Check if requester is a client (User or Patient)
            bool isRequesterClient = requesterRolesList.Any(role => clientRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

            // Check if addressee is a doctor (Doctor or Professional)
            bool isAddresseeDoctor = addresseeRolesList.Any(role => doctorRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

            // Check if addressee is an admin
            bool isAddresseeAdmin = addresseeRolesList.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            // Check if addressee is another client (User or Patient)
            bool isAddresseeClient = addresseeRolesList.Any(role => clientRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

            // Validation rules:
            // 1. Only clients can send friend requests
            // 2. Only doctors can receive friend requests
            // 3. Admins cannot be added as friends
            // 4. Clients cannot add other clients as friends

            if (!isRequesterClient)
            {
                return Results.BadRequest(new { error = "Only clients can send friend requests." });
            }

            if (!isAddresseeDoctor)
            {
                if (isAddresseeAdmin)
                {
                    return Results.BadRequest(new { error = "Cannot add admins as friends." });
                }
                else if (isAddresseeClient)
                {
                    return Results.BadRequest(new { error = "Cannot add other clients as friends. You can only add doctors." });
                }
                else
                {
                    return Results.BadRequest(new { error = "You can only add doctors as friends." });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating user roles for friendship request");
            return Results.BadRequest(new { error = "Failed to validate user roles." });
        }

        try
        {
            var friendship = await friendshipService.SendFriendRequestAsync(currentUserId.Value, dto.AddresseeId);

            // Send notification to the addressee via Notification service
            // Capture user info before starting background task to avoid ObjectDisposedException
            var currentUser = httpContext.User;
            _ = Task.Run(async () =>
            {
                try
                {
                    var requester = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var requesterName = ResolveDisplayName(currentUser, requester?.UserName ?? "Someone");

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    AddInternalServiceKey(httpClient, configuration);

                    var response = await httpClient.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestSent",
                        payload = new
                        {
                            receiverId = dto.AddresseeId,
                            senderId = currentUserId.Value,
                            senderName = requesterName,
                            friendshipId = friendship.Id
                        }
                    });
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
            // Capture user info before starting background task to avoid ObjectDisposedException
            var currentUser = httpContext.User;
            _ = Task.Run(async () =>
            {
                try
                {
                    var accepter = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var accepterName = ResolveDisplayName(currentUser, accepter?.UserName ?? "Someone");

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    AddInternalServiceKey(httpClient, configuration);

                    var response = await httpClient.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestAccepted",
                        payload = new
                        {
                            requesterId = friendship.RequesterId,
                            accepterId = currentUserId.Value,
                            accepterName,
                            friendshipId = friendship.Id
                        }
                    });
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
            // Capture user info before starting background task to avoid ObjectDisposedException
            var currentUser = httpContext.User;
            _ = Task.Run(async () =>
            {
                try
                {
                    var decliner = await chatService.GetUserByIdAsync(currentUserId.Value);
                    var declinerName = ResolveDisplayName(currentUser, decliner?.UserName ?? "Someone");

                    var httpClient = httpClientFactory.CreateClient("NotificationService");
                    AddInternalServiceKey(httpClient, configuration);

                    var response = await httpClient.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "ChatApp",
                        eventName = "FriendRequestDeclined",
                        payload = new
                        {
                            requesterId = friendship.RequesterId,
                            declinerName
                        }
                    });
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

    private static string? ExtractAccessToken(HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private static string ResolveDisplayName(ClaimsPrincipal user, string fallback)
    {
        var customFirstName = user.FindFirstValue("FirstName");
        var customLastName = user.FindFirstValue("LastName");
        var claimFirstName = user.FindFirstValue(ClaimTypes.GivenName);
        var claimLastName = user.FindFirstValue(ClaimTypes.Surname);

        var firstName = customFirstName ?? claimFirstName;
        var lastName = customLastName ?? claimLastName;

        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
        {
            return $"{firstName} {lastName}";
        }

        return user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("name")
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? fallback;
    }

    private static void AddInternalServiceKey(HttpClient client, IConfiguration configuration)
    {
        var key = configuration["InternalServiceKey"] ?? "internal-dev-key";
        client.DefaultRequestHeaders.Remove("X-Internal-Key");
        client.DefaultRequestHeaders.Add("X-Internal-Key", key);
    }
}
