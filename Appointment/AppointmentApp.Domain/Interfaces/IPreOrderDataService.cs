using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing pre-order data collection
/// Handles dynamic form data collection before order confirmation
/// Used for gathering client information specific to the service domain
/// </summary>
public interface IPreOrderDataService
{
    /// <summary>
    /// Creates a new pre-order data entry for an order
    /// Stores dynamic key-value pairs of client information
    /// </summary>
    /// <param name="orderId">ID of the associated order</param>
    /// <param name="clientId">ID of the client</param>
    /// <param name="dataFields">Dictionary of field names and values</param>
    /// <returns>Created pre-order data entry</returns>
    Task<PreOrderData> CreatePreOrderDataAsync(Guid orderId, Guid clientId, Dictionary<string, string> dataFields);

    /// <summary>
    /// Retrieves pre-order data for a specific order
    /// </summary>
    /// <param name="orderId">ID of the order</param>
    /// <returns>Pre-order data if found, null otherwise</returns>
    Task<PreOrderData?> GetPreOrderDataByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Updates an existing pre-order data entry with new field values
    /// Merges new fields with existing ones, overwriting duplicates
    /// </summary>
    /// <param name="preOrderDataId">ID of the pre-order data entry</param>
    /// <param name="dataFields">Dictionary of field names and values to update</param>
    /// <returns>Updated pre-order data entry</returns>
    Task<PreOrderData> UpdatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> dataFields);

    /// <summary>
    /// Marks a pre-order data entry as completed
    /// Indicates that all required data has been collected
    /// </summary>
    /// <param name="preOrderDataId">ID of the pre-order data entry</param>
    /// <returns>Marked pre-order data entry</returns>
    Task<PreOrderData> MarkAsCompletedAsync(Guid preOrderDataId);

    /// <summary>
    /// Validates that a pre-order data entry contains all required fields
    /// </summary>
    /// <param name="preOrderDataId">ID of the pre-order data entry</param>
    /// <param name="requiredFields">Dictionary of required field names</param>
    /// <returns>True if all required fields are present, false otherwise</returns>
    Task<bool> ValidatePreOrderDataAsync(Guid preOrderDataId, Dictionary<string, string> requiredFields);

    /// <summary>
    /// Deletes a pre-order data entry
    /// </summary>
    /// <param name="preOrderDataId">ID of the pre-order data entry to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeletePreOrderDataAsync(Guid preOrderDataId);
}