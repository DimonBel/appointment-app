using AutomationApp.Domain.Entity;

namespace AutomationApp.Repository.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation?> GetActiveByUserIdAsync(Guid userId);
    Task<IEnumerable<Conversation>> GetAllAsync();
    Task<Conversation> AddAsync(Conversation conversation);
    Task<Conversation> UpdateAsync(Conversation conversation);
    Task DeleteAsync(Guid id);
}

public interface IConversationMessageRepository
{
    Task<ConversationMessage?> GetByIdAsync(Guid id);
    Task<IEnumerable<ConversationMessage>> GetByConversationIdAsync(Guid conversationId);
    Task<ConversationMessage> AddAsync(ConversationMessage message);
    Task<IEnumerable<ConversationMessage>> AddRangeAsync(IEnumerable<ConversationMessage> messages);
}

public interface IBookingDraftRepository
{
    Task<BookingDraft?> GetByIdAsync(Guid id);
    Task<BookingDraft?> GetByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<BookingDraft>> GetByUserIdAsync(Guid userId);
    Task<BookingDraft> AddAsync(BookingDraft draft);
    Task<BookingDraft> UpdateAsync(BookingDraft draft);
    Task DeleteAsync(Guid id);
}

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    IConversationRepository ConversationRepository { get; }
    IConversationMessageRepository ConversationMessageRepository { get; }
    IBookingDraftRepository BookingDraftRepository { get; }
}