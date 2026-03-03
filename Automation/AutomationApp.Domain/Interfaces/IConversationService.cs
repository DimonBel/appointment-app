using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;

namespace AutomationApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing AI-powered booking conversations
/// Handles conversational flow, message storage, and conversation state
/// Part of the CQRS-based automation system for conversational appointment booking
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Creates a new conversation for a user
    /// Initializes conversation state and context
    /// </summary>
    /// <param name="userId">ID of the user starting the conversation</param>
    /// <returns>Created conversation with initial state</returns>
    Task<Conversation> CreateConversationAsync(Guid userId);

    /// <summary>
    /// Retrieves a conversation by its ID
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <returns>Conversation if found, null otherwise</returns>
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId);

    /// <summary>
    /// Retrieves the currently active (incomplete) conversation for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Active conversation if found, null otherwise</returns>
    Task<Conversation?> GetActiveConversationByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all conversations for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of user's conversations</returns>
    Task<IEnumerable<Conversation>> GetConversationsByUserIdAsync(Guid userId);

    /// <summary>
    /// Deletes a conversation and all its messages
    /// </summary>
    /// <param name="conversationId">ID of the conversation to delete</param>
    Task DeleteConversationAsync(Guid conversationId);

    /// <summary>
    /// Adds a message to a conversation
    /// Supports user messages, AI responses, and interactive options
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="content">Message content</param>
    /// <param name="isFromUser">True if message is from user, false if from AI</param>
    /// <param name="suggestedOptions">Optional list of suggested response options</param>
    /// <param name="selectedOption">Optional selected option from previous suggestions</param>
    /// <returns>Added conversation message</returns>
    Task<ConversationMessage> AddMessageAsync(Guid conversationId, string content, bool isFromUser, List<string>? suggestedOptions = null, string? selectedOption = null);

    /// <summary>
    /// Retrieves all messages in a conversation ordered by timestamp
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <returns>Collection of conversation messages</returns>
    Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(Guid conversationId);

    /// <summary>
    /// Updates the state of a conversation
    /// Tracks progress through the booking flow
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="newState">New conversation state</param>
    /// <returns>Updated conversation</returns>
    Task<Conversation> UpdateConversationStateAsync(Guid conversationId, ConversationState newState);

    /// <summary>
    /// Updates the context data for a conversation
    /// Stores extracted booking information, user preferences, etc.
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="contextData">Dictionary of context key-value pairs</param>
    /// <returns>Updated conversation</returns>
    Task<Conversation> UpdateConversationContextAsync(Guid conversationId, Dictionary<string, object> contextData);
}

/// <summary>
/// Service interface for LLM (Large Language Model) integration
/// Handles AI message processing, intent detection, and response generation
/// Uses Ollama for local LLM inference
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Processes a user message through the LLM to generate a response
    /// Detects user intent and extracts booking information
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="userMessage">User's message content</param>
    /// <param name="currentState">Current conversation state</param>
    /// <param name="context">Optional context data</param>
    /// <param name="availableProfessionals">List of available professionals for booking</param>
    /// <param name="domainConfigurations">List of available service domains</param>
    /// <param name="onPartialResponse">Optional callback for streaming partial responses</param>
    /// <returns>LLM response with text, suggestions, and extracted data</returns>
    Task<LLMResponse> ProcessUserMessageAsync(
        Guid conversationId,
        string userMessage,
        ConversationState currentState,
        Dictionary<string, object>? context = null,
        List<ProfessionalInfo>? availableProfessionals = null,
        List<DomainConfigurationInfo>? domainConfigurations = null,
        Func<string, Task>? onPartialResponse = null);

    /// <summary>
    /// Generates a personalized greeting message for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Generated greeting message</returns>
    Task<string> GenerateGreetingAsync(Guid userId);

    /// <summary>
    /// Generates booking-related option suggestions
    /// Used to guide users through the booking process
    /// </summary>
    /// <returns>List of booking option suggestions</returns>
    Task<List<string>> GenerateBookingOptionsAsync();
}

/// <summary>
/// Service interface for booking automation workflow
/// Manages the complete booking process from conversation to order creation
/// Integrates with Appointment API for final order submission
/// </summary>
public interface IBookingAutomationService
{
    /// <summary>
    /// Creates a new booking draft for a conversation
    /// Initializes draft with conversation context
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="userId">ID of the user</param>
    /// <returns>Created booking draft</returns>
    Task<BookingDraft> CreateBookingDraftAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// Retrieves a booking draft by its ID
    /// </summary>
    /// <param name="draftId">ID of the booking draft</param>
    /// <returns>Booking draft if found, null otherwise</returns>
    Task<BookingDraft?> GetBookingDraftAsync(Guid draftId);

    /// <summary>
    /// Retrieves the booking draft associated with a conversation
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <returns>Booking draft if found, null otherwise</returns>
    Task<BookingDraft?> GetBookingDraftByConversationIdAsync(Guid conversationId);

    /// <summary>
    /// Updates booking draft information
    /// Allows incremental building of booking details through conversation
    /// </summary>
    /// <param name="draftId">ID of the booking draft</param>
    /// <param name="professionalId">Optional professional ID to set</param>
    /// <param name="serviceType">Optional service type to set</param>
    /// <param name="preferredDateTime">Optional preferred date/time to set</param>
    /// <param name="clientNotes">Optional client notes to set</param>
    /// <returns>Updated booking draft</returns>
    Task<BookingDraft> UpdateBookingDraftAsync(Guid draftId, Guid? professionalId = null, string? serviceType = null, DateTime? preferredDateTime = null, string? clientNotes = null);

    /// <summary>
    /// Submits a completed booking draft to create an actual order
    /// Validates completeness and communicates with Appointment API
    /// </summary>
    /// <param name="draftId">ID of the booking draft</param>
    /// <param name="accessToken">Optional access token for authentication</param>
    /// <returns>Submitted booking draft with order reference</returns>
    Task<BookingDraft> SubmitBookingDraftAsync(Guid draftId, string? accessToken = null);

    /// <summary>
    /// Cancels a booking draft without creating an order
    /// </summary>
    /// <param name="draftId">ID of the booking draft</param>
    /// <returns>Cancelled booking draft</returns>
    Task<BookingDraft> CancelBookingDraftAsync(Guid draftId);

    /// <summary>
    /// Retrieves list of available professionals for booking
    /// Fetches from Appointment API
    /// </summary>
    /// <param name="accessToken">Optional access token for authentication</param>
    /// <returns>List of available professional information</returns>
    Task<List<ProfessionalInfo>> GetAvailableProfessionalsAsync(string? accessToken = null);

    /// <summary>
    /// Retrieves list of available domain configurations
    /// Fetches from Appointment API
    /// </summary>
    /// <param name="accessToken">Optional access token for authentication</param>
    /// <returns>List of domain configuration information</returns>
    Task<List<DomainConfigurationInfo>> GetDomainConfigurationsAsync(string? accessToken = null);

    /// <summary>
    /// Retrieves available time slots for a professional on a specific date
    /// Fetches from Appointment API
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="date">Date to check availability for</param>
    /// <param name="accessToken">Optional access token for authentication</param>
    /// <returns>List of available slot information</returns>
    Task<List<AvailableSlotInfo>> GetAvailableSlotsAsync(Guid professionalId, DateTime date, string? accessToken = null);
}

/// <summary>
/// Information about an available time slot
/// Used for presenting booking options to users
/// </summary>
public class AvailableSlotInfo
{
    /// <summary>
    /// Date of the available slot
    /// </summary>
    public DateTime SlotDate { get; set; }

    /// <summary>
    /// Start time of the slot
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the slot
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Human-readable display label for the slot
    /// </summary>
    public string DisplayLabel { get; set; } = string.Empty;
}

/// <summary>
/// Information about a domain configuration for booking
/// Used for presenting service type options to users
/// </summary>
public class DomainConfigurationInfo
{
    /// <summary>
    /// Unique identifier for the domain configuration
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of domain (Medical, Legal, Consulting, etc.)
    /// </summary>
    public int DomainType { get; set; }

    /// <summary>
    /// Display name of the domain
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the domain
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default appointment duration in minutes
    /// </summary>
    public int DefaultDurationMinutes { get; set; }

    /// <summary>
    /// Required fields for pre-order data collection
    /// </summary>
    public Dictionary<string, string>? RequiredFields { get; set; }
}

/// <summary>
/// Service interface for extracting and validating booking information from user messages
/// Uses NLP techniques to identify booking parameters
/// </summary>
public interface IDataCollectionService
{
    /// <summary>
    /// Extracts booking information from a user message
    /// Analyzes text to identify professional, date, time, service type, etc.
    /// </summary>
    /// <param name="userMessage">User's message text</param>
    /// <param name="currentState">Current conversation state for context</param>
    /// <returns>Dictionary of extracted booking data</returns>
    Task<Dictionary<string, object>> ExtractBookingInfoAsync(string userMessage, ConversationState currentState);

    /// <summary>
    /// Validates that extracted booking information is complete and correct
    /// Checks for required fields and valid values
    /// </summary>
    /// <param name="bookingData">Dictionary of booking data to validate</param>
    /// <returns>True if booking data is valid, false otherwise</returns>
    Task<bool> ValidateBookingInfoAsync(Dictionary<string, object> bookingData);
}

/// <summary>
/// Response from LLM processing
/// Contains generated response, suggestions, and extracted data
/// </summary>
public record LLMResponse
{
    /// <summary>
    /// Generated response text from the LLM
    /// </summary>
    public string ResponseText { get; init; } = string.Empty;

    /// <summary>
    /// Suggested response options for user interaction
    /// </summary>
    public List<string> SuggestedOptions { get; init; } = new();

    /// <summary>
    /// Detected user intent from the message
    /// </summary>
    public UserIntent DetectedIntent { get; init; }

    /// <summary>
    /// Suggested next state for the conversation
    /// </summary>
    public ConversationState? SuggestedNextState { get; init; }

    /// <summary>
    /// Extracted booking data from the message
    /// </summary>
    public Dictionary<string, object>? ExtractedData { get; init; }
}