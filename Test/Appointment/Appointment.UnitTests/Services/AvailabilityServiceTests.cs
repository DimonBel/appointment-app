using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Appointment.UnitTests.Services;

/// <summary>
/// Comprehensive unit tests for AvailabilityService covering all scenarios and functionality
/// Module: Availability & Schedule Module (1.2)
/// </summary>
public class AvailabilityServiceTests
{
    private readonly Mock<IAvailabilityRepository> _mockAvailabilityRepository;
    private readonly Mock<IAvailabilitySlotRepository> _mockAvailabilitySlotRepository;
    private readonly Mock<IProfessionalRepository> _mockProfessionalRepository;
    private readonly AvailabilityService _availabilityService;

    public AvailabilityServiceTests()
    {
        _mockAvailabilityRepository = new Mock<IAvailabilityRepository>();
        _mockAvailabilitySlotRepository = new Mock<IAvailabilitySlotRepository>();
        _mockProfessionalRepository = new Mock<IProfessionalRepository>();

        _availabilityService = new AvailabilityService(
            _mockAvailabilityRepository.Object,
            _mockAvailabilitySlotRepository.Object,
            _mockProfessionalRepository.Object);
    }

    #region CreateAvailabilityAsync Tests

    [Fact]
    public async Task CreateAvailabilityAsync_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dayOfWeek = DayOfWeek.Monday;
        var startTime = TimeSpan.FromHours(9);
        var endTime = TimeSpan.FromHours(17);
        var scheduleType = ScheduleType.Recurring;

        var professional = new Professional { Id = professionalId, IsAvailable = true };

        var createdAvailability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            ScheduleType = scheduleType,
            IsActive = true
        };

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilityRepository.Setup(x => x.CreateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(createdAvailability);

        // Act
        var result = await _availabilityService.CreateAvailabilityAsync(
            professionalId, dayOfWeek, startTime, endTime, scheduleType);

        // Assert
        result.Should().NotBeNull();
        result.ProfessionalId.Should().Be(professionalId);
        result.DayOfWeek.Should().Be(dayOfWeek);
        result.StartTime.Should().Be(startTime);
        result.EndTime.Should().Be(endTime);
        result.ScheduleType.Should().Be(scheduleType);
        result.IsActive.Should().BeTrue();
        _mockAvailabilityRepository.Verify(x => x.CreateAsync(It.IsAny<Availability>()), Times.Once);
    }

    [Fact]
    public async Task CreateAvailabilityAsync_WithNonExistentProfessional_ShouldThrowArgumentException()
    {
        // Arrange
        var professionalId = Guid.NewGuid();

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync((Professional?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.CreateAvailabilityAsync(
                professionalId, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17), ScheduleType.Recurring));

        // Assert
        exception.ParamName.Should().Be(nameof(professionalId));
        exception.Message.Should().Contain("Professional not found");
    }

    [Fact]
    public async Task CreateAvailabilityAsync_WithStartTimeAfterEndTime_ShouldThrowArgumentException()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var startTime = TimeSpan.FromHours(17);
        var endTime = TimeSpan.FromHours(9);

        var professional = new Professional { Id = professionalId };

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.CreateAvailabilityAsync(
                professionalId, DayOfWeek.Monday, startTime, endTime, ScheduleType.Recurring));

        // Assert
        exception.Message.Should().Contain("Start time must be before end time");
    }

    [Fact]
    public async Task CreateAvailabilityAsync_WithEqualTimes_ShouldThrowArgumentException()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var sameTime = TimeSpan.FromHours(12);

        var professional = new Professional { Id = professionalId };

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.CreateAvailabilityAsync(
                professionalId, DayOfWeek.Monday, sameTime, sameTime, ScheduleType.Recurring));

        // Assert
        exception.Message.Should().Contain("Start time must be before end time");
    }

    [Fact]
    public async Task CreateAvailabilityAsync_WithDateRange_ShouldIncludeDateRange()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(30);

        var professional = new Professional { Id = professionalId };

        var createdAvailability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            StartDate = startDate,
            EndDate = endDate
        };

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilityRepository.Setup(x => x.CreateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(createdAvailability);

        // Act
        var result = await _availabilityService.CreateAvailabilityAsync(
            professionalId, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17),
            ScheduleType.DateRange, startDate, endDate);

        // Assert
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
        result.ScheduleType.Should().Be(ScheduleType.DateRange);
    }

    [Fact]
    public async Task CreateAvailabilityAsync_WithOneTimeSchedule_ShouldSetCorrectType()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var oneTimeDate = DateTime.UtcNow.AddDays(1);

        var professional = new Professional { Id = professionalId };

        var createdAvailability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            ScheduleType = ScheduleType.OneTime
        };

        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilityRepository.Setup(x => x.CreateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(createdAvailability);

        // Act
        var result = await _availabilityService.CreateAvailabilityAsync(
            professionalId, DayOfWeek.Wednesday, TimeSpan.FromHours(14), TimeSpan.FromHours(16),
            ScheduleType.OneTime, oneTimeDate, oneTimeDate);

        // Assert
        result.ScheduleType.Should().Be(ScheduleType.OneTime);
    }

    #endregion

    #region GetAvailabilityByIdAsync Tests

    [Fact]
    public async Task GetAvailabilityByIdAsync_WithExistingAvailability_ShouldReturnAvailability()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();
        var availability = new Availability
        {
            Id = availabilityId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync(availability);

        // Act
        var result = await _availabilityService.GetAvailabilityByIdAsync(availabilityId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(availabilityId);
        _mockAvailabilityRepository.Verify(x => x.GetByIdAsync(availabilityId), Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityByIdAsync_WithNonExistentAvailability_ShouldReturnNull()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync((Availability?)null);

        // Act
        var result = await _availabilityService.GetAvailabilityByIdAsync(availabilityId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAvailabilitiesAsync Tests

    [Fact]
    public async Task GetAllAvailabilitiesAsync_ShouldReturnAllAvailabilities()
    {
        // Arrange
        var availabilities = new List<Availability>
        {
            new Availability { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday },
            new Availability { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday },
            new Availability { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Wednesday }
        };

        _mockAvailabilityRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(availabilities);

        // Act
        var result = await _availabilityService.GetAllAvailabilitiesAsync();

        // Assert
        result.Should().HaveCount(3);
        _mockAvailabilityRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAvailabilitiesAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockAvailabilityRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Availability>());

        // Act
        var result = await _availabilityService.GetAllAvailabilitiesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetAvailabilitiesByProfessionalAsync Tests

    [Fact]
    public async Task GetAvailabilitiesByProfessionalAsync_WithValidProfessionalId_ShouldReturnAvailabilities()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var availabilities = new List<Availability>
        {
            new Availability { Id = Guid.NewGuid(), ProfessionalId = professionalId, DayOfWeek = DayOfWeek.Monday },
            new Availability { Id = Guid.NewGuid(), ProfessionalId = professionalId, DayOfWeek = DayOfWeek.Wednesday }
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(availabilities);

        // Act
        var result = await _availabilityService.GetAvailabilitiesByProfessionalAsync(professionalId);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.ProfessionalId == professionalId).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailabilitiesByProfessionalAsync_WithNoAvailabilities_ShouldReturnEmptyList()
    {
        // Arrange
        var professionalId = Guid.NewGuid();

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new List<Availability>());

        // Act
        var result = await _availabilityService.GetAvailabilitiesByProfessionalAsync(professionalId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAvailabilityAsync Tests

    [Fact]
    public async Task UpdateAvailabilityAsync_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();
        var newDayOfWeek = DayOfWeek.Tuesday;
        var newStartTime = TimeSpan.FromHours(10);
        var newEndTime = TimeSpan.FromHours(18);

        var existingAvailability = new Availability
        {
            Id = availabilityId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };

        var updatedAvailability = new Availability
        {
            Id = availabilityId,
            DayOfWeek = newDayOfWeek,
            StartTime = newStartTime,
            EndTime = newEndTime
        };

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync(existingAvailability);
        _mockAvailabilityRepository.Setup(x => x.UpdateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _availabilityService.UpdateAvailabilityAsync(
            availabilityId, newDayOfWeek, newStartTime, newEndTime);

        // Assert
        result.DayOfWeek.Should().Be(newDayOfWeek);
        result.StartTime.Should().Be(newStartTime);
        result.EndTime.Should().Be(newEndTime);
        _mockAvailabilityRepository.Verify(x => x.UpdateAsync(It.IsAny<Availability>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();
        var newStartTime = TimeSpan.FromHours(10);

        var existingAvailability = new Availability
        {
            Id = availabilityId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };

        var updatedAvailability = new Availability
        {
            Id = availabilityId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = newStartTime,
            EndTime = TimeSpan.FromHours(17)
        };

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync(existingAvailability);
        _mockAvailabilityRepository.Setup(x => x.UpdateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _availabilityService.UpdateAvailabilityAsync(availabilityId, startTime: newStartTime);

        // Assert
        result.StartTime.Should().Be(newStartTime);
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
        result.EndTime.Should().Be(TimeSpan.FromHours(17));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithNonExistentAvailability_ShouldThrowArgumentException()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync((Availability?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.UpdateAvailabilityAsync(availabilityId));

        // Assert
        exception.ParamName.Should().Be(nameof(availabilityId));
        exception.Message.Should().Contain("Availability not found");
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithInvalidTimeRange_ShouldThrowArgumentException()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();

        var existingAvailability = new Availability
        {
            Id = availabilityId,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync(existingAvailability);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.UpdateAvailabilityAsync(availabilityId, startTime: TimeSpan.FromHours(18), endTime: TimeSpan.FromHours(9)));

        // Assert
        exception.Message.Should().Contain("Start time must be before end time");
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithEndDate_ShouldUpdateEndDate()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();
        var newEndDate = DateTime.UtcNow.AddDays(60);

        var existingAvailability = new Availability
        {
            Id = availabilityId,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var updatedAvailability = new Availability
        {
            Id = availabilityId,
            EndDate = newEndDate
        };

        _mockAvailabilityRepository.Setup(x => x.GetByIdAsync(availabilityId))
            .ReturnsAsync(existingAvailability);
        _mockAvailabilityRepository.Setup(x => x.UpdateAsync(It.IsAny<Availability>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _availabilityService.UpdateAvailabilityAsync(availabilityId, endDate: newEndDate);

        // Assert
        result.EndDate.Should().Be(newEndDate);
    }

    #endregion

    #region DeleteAvailabilityAsync Tests

    [Fact]
    public async Task DeleteAvailabilityAsync_WithValidAvailabilityId_ShouldReturnTrue()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();

        _mockAvailabilityRepository.Setup(x => x.DeleteAsync(availabilityId))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.DeleteAvailabilityAsync(availabilityId);

        // Assert
        result.Should().BeTrue();
        _mockAvailabilityRepository.Verify(x => x.DeleteAsync(availabilityId), Times.Once);
    }

    [Fact]
    public async Task DeleteAvailabilityAsync_WithInvalidAvailabilityId_ShouldReturnFalse()
    {
        // Arrange
        var availabilityId = Guid.NewGuid();

        _mockAvailabilityRepository.Setup(x => x.DeleteAsync(availabilityId))
            .ReturnsAsync(false);

        // Act
        var result = await _availabilityService.DeleteAvailabilityAsync(availabilityId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetSlotsByDateAsync Tests

    [Fact]
    public async Task GetSlotsByDateAsync_WithExistingSlots_ShouldReturnExistingSlots()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        var existingSlots = new List<AvailabilitySlot>
        {
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9), IsAvailable = true },
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)), IsAvailable = false }
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(professionalId, date))
            .ReturnsAsync(existingSlots);

        // Act
        var result = await _availabilityService.GetSlotsByDateAsync(professionalId, date);

        // Assert
        result.Should().HaveCount(2);
        _mockAvailabilitySlotRepository.Verify(x => x.GetSlotsByDateAsync(professionalId, date), Times.Once);
        _mockAvailabilitySlotRepository.Verify(x => x.GenerateSlotsForDateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetSlotsByDateAsync_WithNoExistingSlots_ShouldGenerateSlots()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;

        var availabilities = new List<Availability>
        {
            new Availability
            {
                Id = Guid.NewGuid(),
                ProfessionalId = professionalId,
                DayOfWeek = dayOfWeek,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                IsActive = true
            }
        };

        var generatedSlots = new List<AvailabilitySlot>
        {
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9) },
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)) }
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(professionalId, date))
            .ReturnsAsync(new List<AvailabilitySlot>());
        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(availabilities);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, It.IsAny<DateTime>()))
            .ReturnsAsync((AvailabilitySlot?)null);
        _mockAvailabilitySlotRepository.Setup(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(professionalId, date))
            .ReturnsAsync(generatedSlots);

        // Act
        var result = await _availabilityService.GetSlotsByDateAsync(professionalId, date);

        // Assert
        _mockAvailabilityRepository.Verify(x => x.GetByProfessionalAsync(professionalId), Times.Once);
        _mockAvailabilitySlotRepository.Verify(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()), Times.AtLeastOnce);
    }

    #endregion

    #region GetAvailableSlotsAsync Tests

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnOnlyAvailableSlots()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        var allSlots = new List<AvailabilitySlot>
        {
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9), IsAvailable = true },
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)), IsAvailable = false },
            new AvailabilitySlot { Id = Guid.NewGuid(), StartTime = TimeSpan.FromHours(10), IsAvailable = true }
        };

        var availableSlots = allSlots.Where(s => s.IsAvailable).ToList();

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(professionalId, date))
            .ReturnsAsync(allSlots);
        _mockAvailabilitySlotRepository.Setup(x => x.GetAvailableSlotsAsync(professionalId, date))
            .ReturnsAsync(availableSlots);

        // Act
        var result = await _availabilityService.GetAvailableSlotsAsync(professionalId, date);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.IsAvailable).Should().BeTrue();
        _mockAvailabilitySlotRepository.Verify(x => x.GetAvailableSlotsAsync(professionalId, date), Times.Once);
    }

    #endregion

    #region IsSlotAvailableAsync Tests

    [Fact]
    public async Task IsSlotAvailableAsync_WithAvailableSlotAndSufficientDuration_ShouldReturnTrue()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow.AddDays(1).Date.Add(TimeSpan.FromHours(10));
        const int durationMinutes = 30;

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = true
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, dateTime))
            .ReturnsAsync(slot);

        // Act
        var result = await _availabilityService.IsSlotAvailableAsync(professionalId, dateTime, durationMinutes);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WithNonExistentSlot_ShouldReturnFalse()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow.AddDays(1).Date.Add(TimeSpan.FromHours(10));
        const int durationMinutes = 30;

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, dateTime))
            .ReturnsAsync((AvailabilitySlot?)null);

        // Act
        var result = await _availabilityService.IsSlotAvailableAsync(professionalId, dateTime, durationMinutes);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WithUnavailableSlot_ShouldReturnFalse()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow.AddDays(1).Date.Add(TimeSpan.FromHours(10));
        const int durationMinutes = 30;

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = false
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, dateTime))
            .ReturnsAsync(slot);

        // Act
        var result = await _availabilityService.IsSlotAvailableAsync(professionalId, dateTime, durationMinutes);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WithInsufficientDuration_ShouldReturnFalse()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow.AddDays(1).Date.Add(TimeSpan.FromHours(10));
        const int durationMinutes = 60; // Requested duration exceeds slot duration

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = true
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, dateTime))
            .ReturnsAsync(slot);

        // Act
        var result = await _availabilityService.IsSlotAvailableAsync(professionalId, dateTime, durationMinutes);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WithExactDuration_ShouldReturnTrue()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow.AddDays(1).Date.Add(TimeSpan.FromHours(10));
        const int durationMinutes = 30;

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = true
        };

        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, dateTime))
            .ReturnsAsync(slot);

        // Act
        var result = await _availabilityService.IsSlotAvailableAsync(professionalId, dateTime, durationMinutes);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GenerateSlotsForDateAsync Tests

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithMatchingAvailability_ShouldGenerateSlots()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            IsActive = true
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, It.IsAny<DateTime>()))
            .ReturnsAsync((AvailabilitySlot?)null);
        _mockAvailabilitySlotRepository.Setup(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().HaveCount(2); // 9:00-9:30 and 9:30-10:00
        _mockAvailabilitySlotRepository.Verify(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithNoMatchingAvailability_ShouldReturnEmptyList()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var differentDayOfWeek = date.DayOfWeek == DayOfWeek.Monday ? DayOfWeek.Tuesday : DayOfWeek.Monday;

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = differentDayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            IsActive = true
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().BeEmpty();
        _mockAvailabilitySlotRepository.Verify(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithInactiveAvailability_ShouldNotGenerateSlots()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            IsActive = false
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithExistingSlots_ShouldSkipExisting()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;
        var slotDateTime = date.Add(TimeSpan.FromHours(9));

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            IsActive = true
        };

        var existingSlot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(9)
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotByDateTimeAsync(professionalId, It.IsAny<DateTime>()))
            .ReturnsAsync(existingSlot);

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().BeEmpty();
        _mockAvailabilitySlotRepository.Verify(x => x.CreateAsync(It.IsAny<AvailabilitySlot>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithStartDateInFuture_ShouldSkipGeneration()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            StartDate = date.AddDays(7),
            IsActive = true
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSlotsForDateAsync_WithEndDatePassed_ShouldSkipGeneration()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;
        var dayOfWeek = date.DayOfWeek;

        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            EndDate = date.AddDays(-1),
            IsActive = true
        };

        _mockAvailabilityRepository.Setup(x => x.GetByProfessionalAsync(professionalId))
            .ReturnsAsync(new[] { availability });

        // Act
        var result = await _availabilityService.GenerateSlotsForDateAsync(professionalId, date);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}