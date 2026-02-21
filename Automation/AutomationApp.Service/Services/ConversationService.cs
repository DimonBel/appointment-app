using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Repository.Interfaces;

namespace AutomationApp.Service.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;

    public ConversationService(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Conversation> CreateConversationAsync(Guid userId)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            State = ConversationState.Greeting,
            ContextData = new Dictionary<string, object>(),
            IsActive = true
        };
        return await _conversationRepository.AddAsync(conversation);
    }

    public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
    {
        return await _conversationRepository.GetByIdAsync(conversationId);
    }

    public async Task<Conversation?> GetActiveConversationByUserIdAsync(Guid userId)
    {
        return await _conversationRepository.GetActiveByUserIdAsync(userId);
    }

    public async Task<ConversationMessage> AddMessageAsync(Guid conversationId, string content, bool isFromUser, List<string>? suggestedOptions = null, string? selectedOption = null)
    {
        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = content,
            IsFromUser = isFromUser,
            SentAt = DateTime.UtcNow,
            SuggestedOptions = suggestedOptions,
            SelectedOption = selectedOption
        };
        return await _messageRepository.AddAsync(message);
    }

    public async Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(Guid conversationId)
    {
        return await _messageRepository.GetByConversationIdAsync(conversationId);
    }

    public async Task<Conversation> UpdateConversationStateAsync(Guid conversationId, ConversationState newState)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            throw new InvalidOperationException($"Conversation with id {conversationId} not found");

        conversation.State = newState;
        conversation.LastActivityAt = DateTime.UtcNow;
        return await _conversationRepository.UpdateAsync(conversation);
    }

    public async Task<Conversation> UpdateConversationContextAsync(Guid conversationId, Dictionary<string, object> contextData)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            throw new InvalidOperationException($"Conversation with id {conversationId} not found");

        conversation.ContextData = contextData;
        conversation.LastActivityAt = DateTime.UtcNow;
        return await _conversationRepository.UpdateAsync(conversation);
    }
}