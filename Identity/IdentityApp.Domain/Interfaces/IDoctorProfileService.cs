using IdentityApp.Domain.DTOs;

namespace IdentityApp.Domain.Interfaces;

public interface IDoctorProfileService
{
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByIdAsync(Guid id);
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByUserIdAsync(Guid userId);
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetAllProfilesAsync();
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetProfilesBySpecialtyAsync(string specialty);
    Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> SearchProfilesAsync(string query);
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> CreateProfileAsync(Guid userId, CreateDoctorProfileDto dto);
    Task<(bool Success, string Message, DoctorProfileDto? Profile)> UpdateProfileAsync(Guid userId, UpdateDoctorProfileDto dto);
    Task<(bool Success, string Message)> DeleteProfileAsync(Guid userId);
}
