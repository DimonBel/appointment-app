using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly IProfessionalRepository _professionalRepository;

    public AvailabilityService(
        IAvailabilityRepository availabilityRepository,
        IAvailabilitySlotRepository availabilitySlotRepository,
        IProfessionalRepository professionalRepository)
    {
        _availabilityRepository = availabilityRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
        _professionalRepository = professionalRepository;
    }

    public async Task<Availability> CreateAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, ScheduleType scheduleType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            ScheduleType = scheduleType,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _availabilityRepository.CreateAsync(availability);
    }

    public async Task<Availability?> GetAvailabilityByIdAsync(Guid availabilityId)
    {
        return await _availabilityRepository.GetByIdAsync(availabilityId);
    }

    public async Task<IEnumerable<Availability>> GetAvailabilitiesByProfessionalAsync(Guid professionalId)
    {
        return await _availabilityRepository.GetByProfessionalAsync(professionalId);
    }

    public async Task<Availability> UpdateAvailabilityAsync(Guid availabilityId, DayOfWeek? dayOfWeek = null, TimeSpan? startTime = null, TimeSpan? endTime = null, DateTime? endDate = null)
    {
        var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
        if (availability == null)
        {
            throw new ArgumentException("Availability not found", nameof(availabilityId));
        }

        if (dayOfWeek.HasValue) availability.DayOfWeek = dayOfWeek.Value;
        if (startTime.HasValue) availability.StartTime = startTime.Value;
        if (endTime.HasValue) availability.EndTime = endTime.Value;
        if (endDate.HasValue) availability.EndDate = endDate.Value;
        if (startTime.HasValue && endTime.HasValue && startTime.Value >= endTime.Value)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        return await _availabilityRepository.UpdateAsync(availability);
    }

    public async Task<bool> DeleteAvailabilityAsync(Guid availabilityId)
    {
        return await _availabilityRepository.DeleteAsync(availabilityId);
    }

    public async Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date)
    {
        return await _availabilitySlotRepository.GetAvailableSlotsAsync(professionalId, date);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes)
    {
        var slot = await _availabilitySlotRepository.GetSlotByDateTimeAsync(professionalId, dateTime);
        if (slot == null || !slot.IsAvailable)
        {
            return false;
        }

        var requestedEndTime = dateTime.TimeOfDay.Add(TimeSpan.FromMinutes(durationMinutes));
        return requestedEndTime <= slot.EndTime;
    }

    public async Task<IEnumerable<AvailabilitySlot>> GenerateSlotsForDateAsync(Guid professionalId, DateTime date)
    {
        var availabilities = await _availabilityRepository.GetByProfessionalAsync(professionalId);
        var dayOfWeek = date.DayOfWeek;

        var matchingAvailabilities = availabilities
            .Where(a => a.DayOfWeek == dayOfWeek && a.IsActive)
            .ToList();

        var slots = new List<AvailabilitySlot>();

        foreach (var availability in matchingAvailabilities)
        {
            if (availability.StartDate.HasValue && date < availability.StartDate.Value.Date)
                continue;
            if (availability.EndDate.HasValue && date > availability.EndDate.Value.Date)
                continue;

            var slotDuration = TimeSpan.FromMinutes(30);
            var currentTime = availability.StartTime;

            while (currentTime.Add(slotDuration) <= availability.EndTime)
            {
                var existingSlot = await _availabilitySlotRepository.GetSlotByDateTimeAsync(professionalId, date.Date.Add(currentTime));
                
                if (existingSlot == null)
                {
                    var slot = new AvailabilitySlot
                    {
                        Id = Guid.NewGuid(),
                        AvailabilityId = availability.Id,
                        SlotDate = date.Date,
                        StartTime = currentTime,
                        EndTime = currentTime.Add(slotDuration),
                        IsAvailable = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    slots.Add(await _availabilitySlotRepository.CreateAsync(slot));
                }

                currentTime = currentTime.Add(slotDuration);
            }
        }

        return slots;
    }
}