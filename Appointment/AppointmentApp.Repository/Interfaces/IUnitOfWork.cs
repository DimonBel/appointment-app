namespace AppointmentApp.Repository.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IOrderRepository OrderRepository { get; }
    IOrderHistoryRepository OrderHistoryRepository { get; }
    IProfessionalRepository ProfessionalRepository { get; }
    IAvailabilityRepository AvailabilityRepository { get; }
    IAvailabilitySlotRepository AvailabilitySlotRepository { get; }
    IDomainConfigurationRepository DomainConfigurationRepository { get; }
    IPreOrderDataRepository PreOrderDataRepository { get; }
    Task<int> SaveChangesAsync();
}