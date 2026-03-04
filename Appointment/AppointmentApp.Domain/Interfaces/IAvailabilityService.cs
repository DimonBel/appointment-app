using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing professional availability schedules and slots
/// Handles creation, modification, and querying of availability rules and time slots
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Creates a new availability rule for a professional
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="dayOfWeek">Day of the week this availability applies to</param>
    /// <param name="startTime">Start time of availability slot</param>
    /// <param name="endTime">End time of availability slot</param>
    /// <param name="scheduleType">Type of schedule (Regular, OneTime, Recurring)</param>
    /// <param name="startDate">Optional start date for time-bound schedules</param>
    /// <param name="endDate">Optional end date for time-bound schedules</param>
    /// <returns>Created availability rule</returns>
    Task<Availability> CreateAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, ScheduleType scheduleType, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Retrieves an availability rule by its ID
    /// </summary>
    /// <param name="availabilityId">ID of the availability rule</param>
    /// <returns>Availability rule if found, null otherwise</returns>
    Task<Availability?> GetAvailabilityByIdAsync(Guid availabilityId);

    /// <summary>
    /// Retrieves all availability rules in the system
    /// </summary>
    /// <returns>Collection of all availability rules</returns>
    Task<IEnumerable<Availability>> GetAllAvailabilitiesAsync();

    /// <summary>
    /// Retrieves all availability rules for a specific professional
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <returns>Collection of availability rules for the professional</returns>
    Task<IEnumerable<Availability>> GetAvailabilitiesByProfessionalAsync(Guid professionalId);

    /// <summary>
    /// Updates an existing availability rule
    /// </summary>
    /// <param name="availabilityId">ID of the availability rule to update</param>
    /// <param name="dayOfWeek">Optional new day of week</param>
    /// <param name="startTime">Optional new start time</param>
    /// <param name="endTime">Optional new end time</param>
    /// <param name="endDate">Optional new end date</param>
    /// <returns>Updated availability rule</returns>
    Task<Availability> UpdateAvailabilityAsync(Guid availabilityId, DayOfWeek? dayOfWeek = null, TimeSpan? startTime = null, TimeSpan? endTime = null, DateTime? endDate = null);

    /// <summary>
    /// Deletes an availability rule
    /// </summary>
    /// <param name="availabilityId">ID of the availability rule to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAvailabilityAsync(Guid availabilityId);

    /// <summary>
    /// Retrieves all time slots for a professional on a specific date
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="date">Date to retrieve slots for</param>
    /// <returns>Collection of availability slots for the date</returns>
    Task<IEnumerable<AvailabilitySlot>> GetSlotsByDateAsync(Guid professionalId, DateTime date);

    /// <summary>
    /// Retrieves only available (not booked) slots for a professional on a specific date
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="date">Date to retrieve available slots for</param>
    /// <returns>Collection of available slots</returns>
    Task<IEnumerable<AvailabilitySlot>> GetAvailableSlotsAsync(Guid professionalId, DateTime date);

    /// <summary>
    /// Checks if a specific time slot is available for booking
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="dateTime">Start date and time of the slot</param>
    /// <param name="durationMinutes">Duration in minutes of the requested slot</param>
    /// <returns>True if the slot is available, false otherwise</returns>
    Task<bool> IsSlotAvailableAsync(Guid professionalId, DateTime dateTime, int durationMinutes);

    /// <summary>
    /// Generates availability slots for a professional on a specific date based on their availability rules
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="date">Date to generate slots for</param>
    /// <returns>Collection of generated availability slots</returns>
    Task<IEnumerable<AvailabilitySlot>> GenerateSlotsForDateAsync(Guid professionalId, DateTime date);
}