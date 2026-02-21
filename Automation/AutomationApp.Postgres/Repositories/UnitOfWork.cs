using AutomationApp.Repository.Interfaces;
using AutomationApp.Postgres.Data;

namespace AutomationApp.Postgres.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AutomationDbContext _context;
    private bool _disposed = false;

    public UnitOfWork(AutomationDbContext context)
    {
        _context = context;
        ConversationRepository = new ConversationRepository(context);
        ConversationMessageRepository = new ConversationMessageRepository(context);
        BookingDraftRepository = new BookingDraftRepository(context);
    }

    public IConversationRepository ConversationRepository { get; }
    public IConversationMessageRepository ConversationMessageRepository { get; }
    public IBookingDraftRepository BookingDraftRepository { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}