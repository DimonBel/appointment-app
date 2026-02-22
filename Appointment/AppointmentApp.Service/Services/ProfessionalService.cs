using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AppointmentApp.Service.Services;

public class ProfessionalService : IProfessionalService
{
    private readonly IProfessionalRepository _professionalRepository;
    private readonly UserManager<AppIdentityUser> _userManager;

    public ProfessionalService(
        IProfessionalRepository professionalRepository,
        UserManager<AppIdentityUser> userManager)
    {
        _professionalRepository = professionalRepository;
        _userManager = userManager;
    }

    public async Task<Professional> CreateProfessionalAsync(Guid userId, string? title = null, string? qualifications = null, string? specialization = null)
    {
        var existingProfessional = await _professionalRepository.GetByUserIdAsync(userId);
        if (existingProfessional != null)
        {
            throw new InvalidOperationException("A professional profile already exists for this user");
        }

        var existingUser = await _userManager.FindByIdAsync(userId.ToString());
        
        // If user doesn't exist in Appointment service, create a basic user entry
        // This happens when the user was created in Identity service but not synced
        if (existingUser == null)
        {
            // Create a minimal user entry with just the ID
            // The full user data should be synced separately or fetched from Identity service
            existingUser = new AppIdentityUser
            {
                Id = userId,
                UserName = $"user_{userId.ToString().Substring(0, 8)}",
                Email = $"user_{userId.ToString().Substring(0, 8)}@placeholder.local",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            var createResult = await _userManager.CreateAsync(existingUser, "TempPassword123!");
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user in Appointment service: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Qualifications = qualifications,
            Specialization = specialization,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _professionalRepository.CreateAsync(professional);
    }

    public async Task<Professional?> GetProfessionalByIdAsync(Guid professionalId)
    {
        return await _professionalRepository.GetByIdAsync(professionalId);
    }

    public async Task<Professional?> GetProfessionalByUserIdAsync(Guid userId)
    {
        return await _professionalRepository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Professional>> GetAllProfessionalsAsync(bool onlyAvailable = true, int page = 1, int pageSize = 20)
    {
        return await _professionalRepository.GetAllAsync(onlyAvailable, page, pageSize);
    }

    public async Task<Professional> UpdateProfessionalAsync(Guid professionalId, string? title = null, string? qualifications = null, string? specialization = null, decimal? hourlyRate = null, int? experienceYears = null, string? bio = null)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        if (title != null) professional.Title = title;
        if (qualifications != null) professional.Qualifications = qualifications;
        if (specialization != null) professional.Specialization = specialization;
        if (hourlyRate.HasValue) professional.HourlyRate = hourlyRate.Value;
        if (experienceYears.HasValue) professional.ExperienceYears = experienceYears.Value;
        if (bio != null) professional.Bio = bio;
        professional.UpdatedAt = DateTime.UtcNow;

        return await _professionalRepository.UpdateAsync(professional);
    }

    public async Task<bool> SetProfessionalAvailabilityAsync(Guid professionalId, bool isAvailable)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        professional.IsAvailable = isAvailable;
        professional.UpdatedAt = DateTime.UtcNow;

        await _professionalRepository.UpdateAsync(professional);
        return true;
    }

    public async Task<bool> DeleteProfessionalAsync(Guid professionalId)
    {
        return await _professionalRepository.DeleteAsync(professionalId);
    }
}