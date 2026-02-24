using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using Appointment.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Appointment.IntegrationTests.Integration;

/// <summary>
/// Integration tests for Pre-Order Data Collection Module (1.5)
/// Tests end-to-end workflows with real database operations
/// </summary>
[Collection("TestDatabase")]
public class PreOrderDataIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IPreOrderDataService _preOrderDataService;
    private readonly IPreOrderDataRepository _preOrderDataRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly IAvailabilityService _availabilityService;

    private AppIdentityUser? _clientUser;
    private AppIdentityUser? _professionalUser;
    private Professional? _professional;
    private Availability? _availability;

    public PreOrderDataIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _preOrderDataService = scope.ServiceProvider.GetRequiredService<IPreOrderDataService>();
        _preOrderDataRepository = scope.ServiceProvider.GetRequiredService<IPreOrderDataRepository>();
        _orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        _professionalRepository = scope.ServiceProvider.GetRequiredService<IProfessionalRepository>();
        _availabilitySlotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        _availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Setup test data
        _clientUser = await _fixture.CreateTestUserAsync("client@test.com", "John", "Doe");
        _professionalUser = await _fixture.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        _professional = await _fixture.CreateTestProfessionalAsync(_professionalUser, "Dr.", "General Medicine");
        _availability = await _fixture.CreateTestAvailabilityAsync(
            _professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Generate slots for next Monday
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Complete Pre-Order Data Creation Workflow

    [Fact]
    public async Task CompletePreOrderDataCreationWorkflow_ShouldCreateAndPersist()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache and fatigue" },
            { "duration", "2 weeks" },
            { "severity", "Moderate" }
        };

        // Act
        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Assert
        preOrderData.Should().NotBeNull();
        preOrderData.Id.Should().NotBeEmpty();
        preOrderData.OrderId.Should().Be(order.Id);
        preOrderData.ClientId.Should().Be(_clientUser.Id);
        preOrderData.IsCompleted.Should().BeFalse();
        preOrderData.DataFields.Should().HaveCount(3);
        preOrderData.DataFields["symptoms"].Should().Be("Headache and fatigue");
        preOrderData.DataFields["duration"].Should().Be("2 weeks");
        preOrderData.DataFields["severity"].Should().Be("Moderate");
        preOrderData.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var retrievedData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        retrievedData.Should().NotBeNull();
        retrievedData!.Id.Should().Be(preOrderData.Id);
        retrievedData.DataFields.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreatePreOrderDataWithEmptyFields_ShouldSucceed()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>();

        // Act
        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Assert
        preOrderData.DataFields.Should().BeEmpty();
        preOrderData.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePreOrderDataWithSingleField_ShouldSucceed()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "reason", "Routine checkup" }
        };

        // Act
        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Assert
        preOrderData.DataFields.Should().HaveCount(1);
        preOrderData.DataFields["reason"].Should().Be("Routine checkup");
    }

    [Fact]
    public async Task CreatePreOrderDataWithSpecialCharacters_ShouldPreserveData()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "allergies", "Peanuts, Shellfish (Mild reaction)" },
            { "medications", "Ibuprofen 200mg (as needed)" },
            { "notes", "Patient reported: \"I feel fine\"" }
        };

        // Act
        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Assert
        preOrderData.DataFields["allergies"].Should().Be("Peanuts, Shellfish (Mild reaction)");
        preOrderData.DataFields["medications"].Should().Be("Ibuprofen 200mg (as needed)");
        preOrderData.DataFields["notes"].Should().Be("Patient reported: \"I feel fine\"");
    }

    #endregion

    #region Pre-Order Data Retrieval Workflow

    [Fact]
    public async Task GetPreOrderDataByOrderIdWorkflow_ShouldReturnCorrectData()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Test symptoms" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Act
        var retrievedData = await _preOrderDataService.GetPreOrderDataByOrderIdAsync(order.Id);

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData!.Id.Should().Be(preOrderData.Id);
        retrievedData.OrderId.Should().Be(order.Id);
        retrievedData.DataFields["symptoms"].Should().Be("Test symptoms");
    }

    [Fact]
    public async Task GetPreOrderDataForNonExistentOrder_ShouldReturnNull()
    {
        // Arrange
        var nonExistentOrderId = Guid.NewGuid();

        // Act
        var retrievedData = await _preOrderDataService.GetPreOrderDataByOrderIdAsync(nonExistentOrderId);

        // Assert
        retrievedData.Should().BeNull();
    }

    #endregion

    #region Pre-Order Data Update Workflow

    [Fact]
    public async Task UpdatePreOrderDataWorkflow_ShouldPersistChanges()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var initialFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" },
            { "duration", "1 week" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, initialFields);

        var updatedFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache and nausea" },
            { "duration", "2 weeks" }
        };

        // Act
        var updatedData = await _preOrderDataService.UpdatePreOrderDataAsync(
            preOrderData.Id, updatedFields);

        // Assert
        updatedData.DataFields["symptoms"].Should().Be("Headache and nausea");
        updatedData.DataFields["duration"].Should().Be("2 weeks");
        updatedData.UpdatedAt.Should().NotBeNull();

        // Verify persistence
        var retrievedData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        retrievedData!.DataFields["symptoms"].Should().Be("Headache and nausea");
    }

    [Fact]
    public async Task UpdatePreOrderDataWithNewFields_ShouldAddFields()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var initialFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, initialFields);

        var additionalFields = new Dictionary<string, string>
        {
            { "duration", "2 weeks" },
            { "severity", "Moderate" }
        };

        // Act
        var updatedData = await _preOrderDataService.UpdatePreOrderDataAsync(
            preOrderData.Id, additionalFields);

        // Assert
        updatedData.DataFields.Should().HaveCount(3);
        updatedData.DataFields.Should().ContainKey("symptoms");
        updatedData.DataFields.Should().ContainKey("duration");
        updatedData.DataFields.Should().ContainKey("severity");
    }

    [Fact]
    public async Task UpdatePreOrderDataExistingField_ShouldOverwriteValue()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var initialFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, initialFields);

        var updateFields = new Dictionary<string, string>
        {
            { "symptoms", "Migraine" }
        };

        // Act
        var updatedData = await _preOrderDataService.UpdatePreOrderDataAsync(
            preOrderData.Id, updateFields);

        // Assert
        updatedData.DataFields.Should().HaveCount(1);
        updatedData.DataFields["symptoms"].Should().Be("Migraine");
    }

    [Fact]
    public async Task UpdateNonExistentPreOrderData_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateFields = new Dictionary<string, string>
        {
            { "symptoms", "Test" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _preOrderDataService.UpdatePreOrderDataAsync(nonExistentId, updateFields));
    }

    #endregion

    #region Mark as Completed Workflow

    [Fact]
    public async Task MarkAsCompletedWorkflow_ShouldUpdateIsCompletedFlag()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Test" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        preOrderData.IsCompleted.Should().BeFalse();

        // Act
        var completedData = await _preOrderDataService.MarkAsCompletedAsync(preOrderData.Id);

        // Assert
        completedData.IsCompleted.Should().BeTrue();
        completedData.UpdatedAt.Should().NotBeNull();

        // Verify persistence
        var retrievedData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        retrievedData!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsCompletedNonExistentData_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _preOrderDataService.MarkAsCompletedAsync(nonExistentId));
    }

    #endregion

    #region Validate Pre-Order Data Workflow

    [Fact]
    public async Task ValidatePreOrderDataWithAllRequiredFields_ShouldReturnTrue()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" },
            { "duration", "2 weeks" },
            { "severity", "Moderate" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" },
            { "severity", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePreOrderDataWithMissingField_ShouldReturnFalse()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataWithEmptyValue_ShouldReturnFalse()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "2 weeks" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataWithWhitespaceValue_ShouldReturnFalse()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "   " },
            { "duration", "2 weeks" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateNonExistentPreOrderData_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            nonExistentId, requiredFields);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataWithEmptyRequirements_ShouldReturnTrue()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>();

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePreOrderDataWithAdditionalFields_ShouldReturnTrue()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" },
            { "duration", "2 weeks" },
            { "severity", "Moderate" },
            { "additional", "Extra info" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" }
        };

        // Act
        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Pre-Order Data Deletion Workflow

    [Fact]
    public async Task DeletePreOrderDataWorkflow_ShouldRemoveFromDatabase()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Test" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, dataFields);

        // Act
        var deleteResult = await _preOrderDataService.DeletePreOrderDataAsync(preOrderData.Id);

        // Assert
        deleteResult.Should().BeTrue();

        // Verify data no longer exists
        var retrievedData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        retrievedData.Should().BeNull();
    }

    #endregion

    #region Complete Pre-Order Data Collection Workflow

    [Fact]
    public async Task CompletePreOrderDataCollectionWorkflow_ShouldTrackAllSteps()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _fixture.CreateTestOrderAsync(
            _clientUser!.Id,
            _professionalUser!.Id,
            scheduledDateTime,
            60);

        // Step 1: Create pre-order data with initial fields
        var initialFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, initialFields);

        preOrderData.IsCompleted.Should().BeFalse();
        preOrderData.DataFields.Should().HaveCount(1);

        // Step 2: Update with additional fields
        var additionalFields = new Dictionary<string, string>
        {
            { "duration", "2 weeks" },
            { "severity", "Moderate" }
        };

        await _preOrderDataService.UpdatePreOrderDataAsync(preOrderData.Id, additionalFields);

        var updatedData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        updatedData!.DataFields.Should().HaveCount(3);

        // Step 3: Validate completeness
        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" },
            { "severity", "" }
        };

        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        isValid.Should().BeTrue();

        // Step 4: Mark as completed
        var completedData = await _preOrderDataService.MarkAsCompletedAsync(preOrderData.Id);

        completedData.IsCompleted.Should().BeTrue();

        // Verify final state
        var finalData = await _preOrderDataRepository.GetByIdAsync(preOrderData.Id);
        finalData!.IsCompleted.Should().BeTrue();
        finalData.DataFields.Should().HaveCount(3);
    }

    #endregion

    #region Helper Methods

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }

    #endregion
}
