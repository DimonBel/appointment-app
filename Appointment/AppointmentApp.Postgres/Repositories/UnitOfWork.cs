using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AppointmentApp.Postgres.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppointmentDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public UnitOfWork(AppointmentDbContext context)
    {
        _context = context;
    }

    public IOrderRepository OrderRepository => new OrderRepository(_context);
    public IOrderHistoryRepository OrderHistoryRepository => new OrderHistoryRepository(_context);
    public IProfessionalRepository ProfessionalRepository => new ProfessionalRepository(_context);
    public IAvailabilityRepository AvailabilityRepository => new AvailabilityRepository(_context);
    public IAvailabilitySlotRepository AvailabilitySlotRepository => new AvailabilitySlotRepository(_context);
    public IDomainConfigurationRepository DomainConfigurationRepository => new DomainConfigurationRepository(_context);
    public IPreOrderDataRepository PreOrderDataRepository => new PreOrderDataRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }
}