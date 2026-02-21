using Microsoft.AspNetCore.SignalR;

namespace AutomationApp.API.Hubs;

public class AutomationHub : Hub
{
    private readonly ILogger<AutomationHub> _logger;

    public AutomationHub(ILogger<AutomationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        _logger.LogInformation($"User {Context.ConnectionId} joined conversation {conversationId}");
        
        // Send join confirmation
        await Clients.Caller.SendAsync("JoinedConversation", conversationId);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        _logger.LogInformation($"User {Context.ConnectionId} left conversation {conversationId}");
    }

    // Send AI response to all participants in a conversation
    public async Task BroadcastMessage(Guid conversationId, object message)
    {
        await Clients.Group($"conversation-{conversationId}").SendAsync("ReceiveMessage", message);
    }

    // Send typing indicator
    public async Task SendTypingIndicator(Guid conversationId, bool isTyping)
    {
        await Clients.Group($"conversation-{conversationId}").SendAsync("TypingIndicator", isTyping);
    }

    // Send conversation state update
    public async Task UpdateConversationState(Guid conversationId, string state)
    {
        await Clients.Group($"conversation-{conversationId}").SendAsync("ConversationStateChanged", state);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}