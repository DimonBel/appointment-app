using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing professional profiles
/// Handles creation, modification, and querying of professional information
/// Professionals are users who provide services and can be booked for appointments
/// </summary>
public interface IProfessionalService
{
    /// <summary>
    /// Creates a new professional profile for a user
    /// Links a user account to professional capabilities
    /// </summary>
    /// <param name="userId">ID of the user to create professional profile for</param>
    /// <param name="title">Optional professional title (e.g., Dr., Attorney, Consultant)</param>
    /// <param name="qualifications">Optional qualifications and credentials</param>
    /// <param name="specialization">Optional area of specialization</param>
    /// <returns>Created professional profile</returns>
    Task<Professional> CreateProfessionalAsync(Guid userId, string? title = null, string? qualifications = null, string? specialization = null);

    /// <summary>
    /// Retrieves a professional by their profile ID
    /// </summary>
    /// <param name="professionalId">ID of the professional profile</param>
    /// <returns>Professional if found, null otherwise</returns>
    Task<Professional?> GetProfessionalByIdAsync(Guid professionalId);

    /// <summary>
    /// Retrieves a professional by their user account ID
    /// </summary>
    /// <param name="userId">ID of the user account</param>
    /// <returns>Professional if found, null otherwise</returns>
    Task<Professional?> GetProfessionalByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all professionals with optional filtering and pagination
    /// </summary>
    /// <param name="onlyAvailable">If true, only returns available professionals</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of professionals</returns>
    Task<IEnumerable<Professional>> GetAllProfessionalsAsync(bool onlyAvailable = true, int page = 1, int pageSize = 20);

    /// <summary>
    /// Updates a professional's profile information
    /// </summary>
    /// <param name="professionalId">ID of the professional to update</param>
    /// <param name="title">Optional new title</param>
    /// <param name="qualifications">Optional new qualifications</param>
    /// <param name="specialization">Optional new specialization</param>
    /// <param name="hourlyRate">Optional new hourly rate</param>
    /// <param name="experienceYears">Optional new years of experience</param>
    /// <param name="bio">Optional new biography</param>
    /// <returns>Updated professional profile</returns>
    Task<Professional> UpdateProfessionalAsync(Guid professionalId, string? title = null, string? qualifications = null, string? specialization = null, decimal? hourlyRate = null, int? experienceYears = null, string? bio = null);

    /// <summary>
    /// Sets a professional's availability status
    /// Controls whether the professional can receive new bookings
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="isAvailable">True to make available, false to make unavailable</param>
    /// <returns>True if status updated successfully, false otherwise</returns>
    Task<bool> SetProfessionalAvailabilityAsync(Guid professionalId, bool isAvailable);

    /// <summary>
    /// Deletes a professional profile
    /// Does not delete the associated user account
    /// </summary>
    /// <param name="professionalId">ID of the professional to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteProfessionalAsync(Guid professionalId);

    /// <summary>
    /// Retrieves all unique specializations from the database
    /// Used for populating dropdown selectors in the UI
    /// </summary>
    /// <returns>List of unique specializations</returns>
    Task<IEnumerable<string>> GetAllSpecializationsAsync();
}