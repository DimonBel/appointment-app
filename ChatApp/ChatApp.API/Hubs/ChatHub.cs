using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Domain.Interfaces;
using ChatApp.Domain.Entity;

namespace ChatApp.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly static Dictionary<Guid, HashSet<string>> _connections = new();
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            // Add this connection to the user's connection set
            if (!_connections.ContainsKey(userGuid))
            {
                _connections[userGuid] = new HashSet<string>();
            }
            _connections[userGuid].Add(Context.ConnectionId);

            // Add to user's personal group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, userGuid.ToString());
            
            _logger.LogInformation($"User {userGuid} connected with connection {Context.ConnectionId}. Total connections: {_connections[userGuid].Count}");
            
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
            if (_connections.ContainsKey(userGuid))
            {
                _connections[userGuid].Remove(Context.ConnectionId);
                
                // If no more connections for this user, notify offline
                if (_connections[userGuid].Count == 0)
                {
                    _connections.Remove(userGuid);
                    await Clients.All.SendAsync("UserOffline", userGuid);
                    _logger.LogInformation($"User {userGuid} went offline");
                }
                else
                {
                    _logger.LogInformation($"User {userGuid} disconnected connection {Context.ConnectionId}. Remaining connections: {_connections[userGuid].Count}");
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
            if (_connections.ContainsKey(receiverId))
            {
                foreach (var connectionId in _connections[receiverId])
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", 
                        savedMessage.SenderId.ToString(), 
                        savedMessage.Content,
                        savedMessage.Id.ToString(),
                        savedMessage.CreatedAt.ToString("o"));
                }
                _logger.LogInformation($"Message sent to {receiverId} via {_connections[receiverId].Count} connections");
            }
            else
            {
                _logger.LogWarning($"Receiver {receiverId} not connected");
            }

            // Send confirmation to sender's connections with complete message data
            if (_connections.ContainsKey(senderGuid))
            {
                foreach (var connectionId in _connections[senderGuid])
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
        if (_connections.ContainsKey(receiverId))
        {
            foreach (var connectionId in _connections[receiverId])
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", senderGuid);
            }
        }
    }
}