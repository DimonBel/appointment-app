using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Interfaces;

public interface IConversationService
{
    Task<Conversation> CreateConversationAsync(Guid userId);
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
    Task<Conversation?> GetActiveConversationByUserIdAsync(Guid userId);
    Task<ConversationMessage> AddMessageAsync(Guid conversationId, string content, bool isFromUser, List<string>? suggestedOptions = null, string? selectedOption = null);
    Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(Guid conversationId);
    Task<Conversation> UpdateConversationStateAsync(Guid conversationId, ConversationState newState);
    Task<Conversation> UpdateConversationContextAsync(Guid conversationId, Dictionary<string, object> contextData);
}

public interface ILLMService
{
    Task<LLMResponse> ProcessUserMessageAsync(Guid conversationId, string userMessage, ConversationState currentState, Dictionary<string, object>? context = null, List<ProfessionalInfo>? availableProfessionals = null, List<DomainConfigurationInfo>? domainConfigurations = null);
    Task<string> GenerateGreetingAsync(Guid userId);
    Task<List<string>> GenerateBookingOptionsAsync();
}

public interface IBookingAutomationService
{
    Task<BookingDraft> CreateBookingDraftAsync(Guid conversationId, Guid userId);
    Task<BookingDraft?> GetBookingDraftAsync(Guid draftId);
    Task<BookingDraft?> GetBookingDraftByConversationIdAsync(Guid conversationId);
    Task<BookingDraft> UpdateBookingDraftAsync(Guid draftId, Guid? professionalId = null, string? serviceType = null, DateTime? preferredDateTime = null, string? clientNotes = null);
    Task<BookingDraft> SubmitBookingDraftAsync(Guid draftId);
    Task<BookingDraft> CancelBookingDraftAsync(Guid draftId);
    Task<List<ProfessionalInfo>> GetAvailableProfessionalsAsync();
    Task<List<DomainConfigurationInfo>> GetDomainConfigurationsAsync();
}

public class DomainConfigurationInfo
{
    public Guid Id { get; set; }
    public int DomainType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultDurationMinutes { get; set; }
    public Dictionary<string, string>? RequiredFields { get; set; }
}

public interface IDataCollectionService
{
    Task<Dictionary<string, object>> ExtractBookingInfoAsync(string userMessage, ConversationState currentState);
    Task<bool> ValidateBookingInfoAsync(Dictionary<string, object> bookingData);
}

public record LLMResponse
{
    public string ResponseText { get; init; } = string.Empty;
    public List<string> SuggestedOptions { get; init; } = new();
    public UserIntent DetectedIntent { get; init; }
    public ConversationState? SuggestedNextState { get; init; }
    public Dictionary<string, object>? ExtractedData { get; init; }
}