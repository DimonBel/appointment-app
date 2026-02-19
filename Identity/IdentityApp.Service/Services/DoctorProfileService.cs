using System.Text.Json;
using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using IdentityApp.Repository.Interfaces;

namespace IdentityApp.Service.Services;

public class DoctorProfileService : IDoctorProfileService
{
    private readonly IDoctorProfileRepository _profileRepository;

    public DoctorProfileService(IDoctorProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByIdAsync(Guid id)
    {
        var profile = await _profileRepository.GetByIdAsync(id);
        if (profile == null)
        {
            return (false, "Profile not found", null);
        }

        return (true, "Profile retrieved successfully", MapToDto(profile));
    }

    public async Task<(bool Success, string Message, DoctorProfileDto? Profile)> GetProfileByUserIdAsync(Guid userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            return (false, "Profile not found", null);
        }

        return (true, "Profile retrieved successfully", MapToDto(profile));
    }

    public async Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetAllProfilesAsync()
    {
        var profiles = await _profileRepository.GetAllAsync();
        var dtos = profiles.Select(MapToDto);
        return (true, "Profiles retrieved successfully", dtos);
    }

    public async Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> GetProfilesBySpecialtyAsync(string specialty)
    {
        var profiles = await _profileRepository.GetBySpecialtyAsync(specialty);
        var dtos = profiles.Select(MapToDto);
        return (true, "Profiles retrieved successfully", dtos);
    }

    public async Task<(bool Success, string Message, IEnumerable<DoctorProfileDto> Profiles)> SearchProfilesAsync(string query)
    {
        var profiles = await _profileRepository.SearchAsync(query);
        var dtos = profiles.Select(MapToDto);
        return (true, "Profiles retrieved successfully", dtos);
    }

    public async Task<(bool Success, string Message, DoctorProfileDto? Profile)> CreateProfileAsync(Guid userId, CreateDoctorProfileDto dto)
    {
        // Check if profile already exists
        var existing = await _profileRepository.GetByUserIdAsync(userId);
        if (existing != null)
        {
            return (false, "Profile already exists for this user", null);
        }

        var profile = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Specialty = dto.Specialty,
            Bio = dto.Bio,
            Qualifications = dto.Qualifications,
            YearsOfExperience = dto.YearsOfExperience,
            Services = JsonSerializer.Serialize(dto.Services),
            ConsultationFee = dto.ConsultationFee,
            Languages = JsonSerializer.Serialize(dto.Languages),
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            IsAvailableForAppointments = true
        };

        var created = await _profileRepository.CreateAsync(profile);
        return (true, "Profile created successfully", MapToDto(created));
    }

    public async Task<(bool Success, string Message, DoctorProfileDto? Profile)> UpdateProfileAsync(Guid userId, UpdateDoctorProfileDto dto)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            return (false, "Profile not found", null);
        }

        // Update fields
        if (dto.Specialty != null) profile.Specialty = dto.Specialty;
        if (dto.Bio != null) profile.Bio = dto.Bio;
        if (dto.Qualifications != null) profile.Qualifications = dto.Qualifications;
        profile.YearsOfExperience = dto.YearsOfExperience;
        if (dto.Services != null) profile.Services = JsonSerializer.Serialize(dto.Services);
        if (dto.ConsultationFee.HasValue) profile.ConsultationFee = dto.ConsultationFee;
        if (dto.Languages != null) profile.Languages = JsonSerializer.Serialize(dto.Languages);
        if (dto.Address != null) profile.Address = dto.Address;
        if (dto.City != null) profile.City = dto.City;
        if (dto.Country != null) profile.Country = dto.Country;
        if (dto.IsAvailableForAppointments.HasValue) profile.IsAvailableForAppointments = dto.IsAvailableForAppointments.Value;

        var updated = await _profileRepository.UpdateAsync(profile);
        return (true, "Profile updated successfully", MapToDto(updated));
    }

    public async Task<(bool Success, string Message)> DeleteProfileAsync(Guid userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            return (false, "Profile not found");
        }

        await _profileRepository.DeleteAsync(profile.Id);
        return (true, "Profile deleted successfully");
    }

    private DoctorProfileDto MapToDto(DoctorProfile profile)
    {
        List<string> services = new();
        List<string> languages = new();

        try
        {
            if (!string.IsNullOrEmpty(profile.Services))
                services = JsonSerializer.Deserialize<List<string>>(profile.Services) ?? new();
            if (!string.IsNullOrEmpty(profile.Languages))
                languages = JsonSerializer.Deserialize<List<string>>(profile.Languages) ?? new();
        }
        catch
        {
            // If deserialization fails, use empty lists
        }

        UserInfoDto? userInfo = null;
        if (profile.User != null)
        {
            userInfo = new UserInfoDto
            {
                Id = profile.User.Id,
                FirstName = profile.User.FirstName,
                LastName = profile.User.LastName,
                Email = profile.User.Email,
                UserName = profile.User.UserName,
                PhoneNumber = profile.User.PhoneNumber
            };
        }

        return new DoctorProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            User = userInfo,
            Specialty = profile.Specialty,
            Bio = profile.Bio,
            Qualifications = profile.Qualifications,
            YearsOfExperience = profile.YearsOfExperience,
            Services = services,
            ConsultationFee = profile.ConsultationFee,
            Languages = languages,
            Address = profile.Address,
            City = profile.City,
            Country = profile.Country,
            IsAvailableForAppointments = profile.IsAvailableForAppointments,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
