using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Domain.Interfaces;
using ChatApp.Domain.Entity;
using System.Collections.Concurrent;

namespace ChatApp.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly static ConcurrentDictionary<Guid, ConcurrentBag<string>> _connections = new();
    private readonly IChatService _chatService;
    private readonly IFriendshipService _friendshipService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, IFriendshipService friendshipService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _friendshipService = friendshipService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            // Add this connection to the user's connection set
            _connections.AddOrUpdate(userGuid,
                _ => new ConcurrentBag<string> { Context.ConnectionId },
                (_, bag) => { bag.Add(Context.ConnectionId); return bag; });

            // Add to user's personal group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, userGuid.ToString());

            var connCount = _connections.TryGetValue(userGuid, out var conns) ? conns.Count : 0;
            _logger.LogInformation($"User {userGuid} connected with connection {Context.ConnectionId}. Total connections: {connCount}");

            // Notify all clients that this user is online
            await Clients.All.SendAsync("UserOnline", userGuid);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            // Remove this specific connection
            if (_connections.TryGetValue(userGuid, out var userConns))
            {
                var updated = new ConcurrentBag<string>(userConns.Where(c => c != Context.ConnectionId));
                _connections.TryUpdate(userGuid, updated, userConns);

                if (updated.IsEmpty)
                {
                    _connections.TryRemove(userGuid, out _);
                    await Clients.All.SendAsync("UserOffline", userGuid);
                    _logger.LogInformation($"User {userGuid} went offline");
                }
                else
                {
                    _logger.LogInformation($"User {userGuid} disconnected connection {Context.ConnectionId}. Remaining connections: {updated.Count}");
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGuid.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid receiverId, string message)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId) || !Guid.TryParse(senderId, out var senderGuid))
        {
            _logger.LogWarning($"Invalid sender ID: {senderId}");
            return;
        }

        try
        {
            // Check if users are friends before allowing message
            var areFriends = await _friendshipService.AreFriendsAsync(senderGuid, receiverId);
            if (!areFriends)
            {
                _logger.LogWarning("User {SenderId} tried to message non-friend {ReceiverId}", senderGuid, receiverId);
                await Clients.Caller.SendAsync("MessageError", "You can only send messages to friends. Send a friend request first.");
                return;
            }

            // Save message to database first
            var savedMessage = await _chatService.SendMessageAsync(senderGuid, receiverId, message);

            _logger.LogInformation($"Message saved: {senderGuid} -> {receiverId}: {message}");

            // Create message data object to send
            var messageData = new
            {
                id = savedMessage.Id,
                senderId = savedMessage.SenderId,
                receiverId = savedMessage.ReceiverId,
                content = savedMessage.Content,
                createdAt = savedMessage.CreatedAt,
                chatId = receiverId.ToString() // For receiver, the chatId is the sender
            };

            // Send to all receiver's connections
            if (_connections.TryGetValue(receiverId, out var receiverConns))
            {
                foreach (var connectionId in receiverConns)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage",
                        savedMessage.SenderId.ToString(),
                        savedMessage.Content,
                        savedMessage.Id.ToString(),
                        savedMessage.CreatedAt.ToString("o"));
                }
                _logger.LogInformation($"Message sent to {receiverId} via {receiverConns.Count} connections");
            }
            else
            {
                _logger.LogWarning($"Receiver {receiverId} not connected");
            }

            // Send confirmation to sender's connections with complete message data
            if (_connections.TryGetValue(senderGuid, out var senderConns))
            {
                foreach (var connectionId in senderConns)
                {
                    await Clients.Client(connectionId).SendAsync("MessageSent",
                        savedMessage.ReceiverId.ToString(),
                        savedMessage.Content,
                        savedMessage.Id.ToString(),
                        savedMessage.CreatedAt.ToString("o"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message from {senderGuid} to {receiverId}: {ex.Message}");
            // Send error back to sender with details
            await Clients.Caller.SendAsync("MessageError", ex.Message);
            throw; // Re-throw to let SignalR handle it
        }
    }

    public async Task SendTypingNotification(Guid receiverId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId) || !Guid.TryParse(senderId, out var senderGuid))
        {
            return;
        }

        // Send typing notification to all receiver's connections
        if (_connections.TryGetValue(receiverId, out var typingConns))
        {
            foreach (var connectionId in typingConns)
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", senderGuid);
            }
        }
    }

    public async Task MarkMessageAsRead(Guid messageId, Guid senderId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return;

        // Notify sender that their message was read
        if (_connections.TryGetValue(senderId, out var senderConns))
        {
            foreach (var connectionId in senderConns)
            {
                await Clients.Client(connectionId).SendAsync("MessageRead", messageId, userGuid);
            }
        }
    }
}