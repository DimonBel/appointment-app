using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Domain.Interfaces;

public interface IPreOrderDataService
{
    Task<PreOrderData> CreatePreOrderDataAsync(Guid orderId, Guid clientId, Dictionary<string, string> dataFields);
    Task<PreOrderData?> GetPreOrderDataByOrderIdAsync(Guid orderId);
    Task<PreOrderData> UpdatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> dataFields);
    Task<PreOrderData> MarkAsCompletedAsync(Guid preOrderDataId);
    Task<bool> ValidatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> requiredFields);
    Task<bool> DeletePreOrderDataAsync(Guid preOrderDataId);
}