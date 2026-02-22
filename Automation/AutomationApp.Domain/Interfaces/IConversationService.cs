using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Interfaces;

public interface IConversationService
{
    Task<Conversation> CreateConversationAsync(Guid userId);
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
    Task<Conversation?> GetActiveConversationByUserIdAsync(Guid userId);
    Task<IEnumerable<Conversation>> GetConversationsByUserIdAsync(Guid userId);
    Task DeleteConversationAsync(Guid conversationId);
    Task<ConversationMessage> AddMessageAsync(Guid conversationId, string content, bool isFromUser, List<string>? suggestedOptions = null, string? selectedOption = null);
    Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(Guid conversationId);
    Task<Conversation> UpdateConversationStateAsync(Guid conversationId, ConversationState newState);
    Task<Conversation> UpdateConversationContextAsync(Guid conversationId, Dictionary<string, object> contextData);
}

public interface ILLMService
{
    Task<LLMResponse> ProcessUserMessageAsync(
        Guid conversationId,
        string userMessage,
        ConversationState currentState,
        Dictionary<string, object>? context = null,
        List<ProfessionalInfo>? availableProfessionals = null,
        List<DomainConfigurationInfo>? domainConfigurations = null,
        Func<string, Task>? onPartialResponse = null);
    Task<string> GenerateGreetingAsync(Guid userId);
    Task<List<string>> GenerateBookingOptionsAsync();
}

public interface IBookingAutomationService
{
    Task<BookingDraft> CreateBookingDraftAsync(Guid conversationId, Guid userId);
    Task<BookingDraft?> GetBookingDraftAsync(Guid draftId);
    Task<BookingDraft?> GetBookingDraftByConversationIdAsync(Guid conversationId);
    Task<BookingDraft> UpdateBookingDraftAsync(Guid draftId, Guid? professionalId = null, string? serviceType = null, DateTime? preferredDateTime = null, string? clientNotes = null);
    Task<BookingDraft> SubmitBookingDraftAsync(Guid draftId, string? accessToken = null);
    Task<BookingDraft> CancelBookingDraftAsync(Guid draftId);
    Task<List<ProfessionalInfo>> GetAvailableProfessionalsAsync(string? accessToken = null);
    Task<List<DomainConfigurationInfo>> GetDomainConfigurationsAsync(string? accessToken = null);
    Task<List<AvailableSlotInfo>> GetAvailableSlotsAsync(Guid professionalId, DateTime date, string? accessToken = null);
}

public class AvailableSlotInfo
{
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
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