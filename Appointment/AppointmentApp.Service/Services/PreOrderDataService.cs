using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

public class PreOrderDataService : IPreOrderDataService
{
    private readonly IPreOrderDataRepository _preOrderDataRepository;

    public PreOrderDataService(IPreOrderDataRepository preOrderDataRepository)
    {
        _preOrderDataRepository = preOrderDataRepository;
    }

    public async Task<PreOrderData> CreatePreOrderDataAsync(Guid orderId, Guid clientId, Dictionary<string, string> dataFields)
    {
        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = dataFields,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        return await _preOrderDataRepository.CreateAsync(preOrderData);
    }

    public async Task<PreOrderData?> GetPreOrderDataByOrderIdAsync(Guid orderId)
    {
        return await _preOrderDataRepository.GetByOrderIdAsync(orderId);
    }

    public async Task<PreOrderData> UpdatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> dataFields)
    {
        var preOrderData = await _preOrderDataRepository.GetByIdAsync(preOrderDataId);
        if (preOrderData == null)
        {
            throw new ArgumentException("Pre-order data not found", nameof(preOrderDataId));
        }

        foreach (var field in dataFields)
        {
            if (preOrderData.DataFields.ContainsKey(field.Key))
            {
                preOrderData.DataFields[field.Key] = field.Value;
            }
            else
            {
                preOrderData.DataFields.Add(field.Key, field.Value);
            }
        }

        preOrderData.UpdatedAt = DateTime.UtcNow;

        return await _preOrderDataRepository.UpdateAsync(preOrderData);
    }

    public async Task<PreOrderData> MarkAsCompletedAsync(Guid preOrderDataId)
    {
        var preOrderData = await _preOrderDataRepository.GetByIdAsync(preOrderDataId);
        if (preOrderData == null)
        {
            throw new ArgumentException("Pre-order data not found", nameof(preOrderDataId));
        }

        preOrderData.IsCompleted = true;
        preOrderData.UpdatedAt = DateTime.UtcNow;

        return await _preOrderDataRepository.UpdateAsync(preOrderData);
    }

    public async Task<bool> ValidatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> requiredFields)
    {
        var preOrderData = await _preOrderDataRepository.GetByIdAsync(preOrderDataId);
        if (preOrderData == null)
        {
            return false;
        }

        foreach (var requiredField in requiredFields)
        {
            if (!preOrderData.DataFields.ContainsKey(requiredField.Key) ||
                string.IsNullOrWhiteSpace(preOrderData.DataFields[requiredField.Key]))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> DeletePreOrderDataAsync(Guid preOrderDataId)
    {
        return await _preOrderDataRepository.DeleteAsync(preOrderDataId);
    }
}