using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for doctor/professional profile management
/// Handles creation, retrieval, update, and search of doctor profiles
/// </summary>
public interface IDoctorProfileService
{
    /// <summary>
    /// Retrieves a doctor profile by its ID
    /// </summary>
    /// <param name="id">ID of the doctor profile</param>
    /// <returns>Tuple with success status, message, and profile data if successful</returns>
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a doctor profile by user account ID
    /// </summary>
    /// <param name="userId">ID of the user account</param>
    /// <returns>Tuple with success status, message, and profile data if successful</returns>
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all doctor profiles in the system
    /// </summary>
    /// <returns>Tuple with success status, message, and collection of profiles</returns>
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetAllProfilesAsync();

    /// <summary>
    /// Retrieves doctor profiles filtered by specialty
    /// </summary>
    /// <param name="specialty">Medical specialty to filter by</param>
    /// <returns>Tuple with success status, message, and collection of matching profiles</returns>
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetProfilesBySpecialtyAsync(string specialty);

    /// <summary>
    /// Searches doctor profiles by name, specialty, or other fields
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <returns>Tuple with success status, message, and collection of matching profiles</returns>
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> SearchProfilesAsync(string query);

    /// <summary>
    /// Creates a new doctor profile for a user
    /// </summary>
    /// <param name="userId">ID of the user to create profile for</param>
    /// <param name="dto">Doctor profile creation data</param>
    /// <returns>Tuple with success status, message, and created profile if successful</returns>
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> CreateProfileAsync(Guid userId, CreateDoctorProfileDto dto);

    /// <summary>
    /// Updates an existing doctor profile
    /// </summary>
    /// <param name="userId">ID of the user who owns the profile</param>
    /// <param name="dto">Updated doctor profile data</param>
    /// <returns>Tuple with success status, message, and updated profile if successful</returns>
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> UpdateProfileAsync(Guid userId, UpdateDoctorProfileDto dto);

    /// <summary>
    /// Deletes a doctor profile
    /// </summary>
    /// <param name="userId">ID of the user who owns the profile</param>
    /// <returns>Tuple with success status and message</returns>
    Task<(bool Success, string Message)> DeleteProfileAsync(Guid userId);
}