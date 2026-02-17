using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IPreOrderDataRepository
{
    Task<PreOrderData?> GetByIdAsync(Guid id);
    Task<PreOrderData?> GetByOrderIdAsync(Guid orderId);
    Task<PreOrderData> CreateAsync(PreOrderData preOrderData);
    Task<PreOrderData> UpdateAsync(PreOrderData preOrderData);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}