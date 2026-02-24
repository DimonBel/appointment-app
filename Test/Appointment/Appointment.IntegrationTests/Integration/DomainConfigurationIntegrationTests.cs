using System;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using Appointment.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Appointment.IntegrationTests.Integration;

/// <summary>
/// Integration tests for Domain Configuration Module (1.4)
/// Tests end-to-end workflows with real database operations
/// </summary>
[Collection("TestDatabase")]
public class DomainConfigurationIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IDomainConfigurationService _domainConfigurationService;
    private readonly IDomainConfigurationRepository _domainConfigurationRepository;

    public DomainConfigurationIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _domainConfigurationService = scope.ServiceProvider.GetRequiredService<IDomainConfigurationService>();
        _domainConfigurationRepository = scope.ServiceProvider.GetRequiredService<IDomainConfigurationRepository>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Complete Domain Configuration Creation Workflow

    [Fact]
    public async Task CompleteDomainConfigurationCreationWorkflow_ShouldCreateAndPersist()
    {
        // Arrange
        var domainType = DomainType.Medical;
        var name = "General Medical Consultation";
        var description = "Standard medical consultation with a general practitioner";
        var defaultDurationMinutes = 30;

        // Act
        var domainConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, description, defaultDurationMinutes);

        // Assert
        domainConfig.Should().NotBeNull();
        domainConfig.Id.Should().NotBeEmpty();
        domainConfig.DomainType.Should().Be(domainType);
        domainConfig.Name.Should().Be(name);
        domainConfig.Description.Should().Be(description);
        domainConfig.DefaultDurationMinutes.Should().Be(defaultDurationMinutes);
        domainConfig.IsActive.Should().BeTrue();
        domainConfig.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var retrievedConfig = await _domainConfigurationRepository.GetByIdAsync(domainConfig.Id);
        retrievedConfig.Should().NotBeNull();
        retrievedConfig!.Id.Should().Be(domainConfig.Id);
        retrievedConfig.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateDomainConfigurationWithoutDescription_ShouldSucceed()
    {
        // Arrange
        var domainType = DomainType.Legal;
        var name = "Legal Consultation";

        // Act
        var domainConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name);

        // Assert
        domainConfig.Description.Should().BeNull();
        domainConfig.DefaultDurationMinutes.Should().Be(60); // Default value
    }

    [Fact]
    public async Task CreateDomainConfigurationWithInvalidName_ShouldThrowException()
    {
        // Arrange
        var domainType = DomainType.Consulting;
        var invalidName = "   "; // Whitespace only

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.CreateDomainConfigurationAsync(domainType, invalidName));
    }

    #endregion

    #region Different Domain Types Workflow

    [Fact]
    public async Task CreateMultipleDomainTypesWorkflow_ShouldSupportAllTypes()
    {
        // Arrange & Act
        var medicalConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "Medical Service", "Medical related services", 45);

        var legalConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Legal, "Legal Service", "Legal consultation services", 60);

        var consultingConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Consulting, "Consulting Service", "Business consulting", 30);

        // Assert
        medicalConfig.DomainType.Should().Be(DomainType.Medical);
        legalConfig.DomainType.Should().Be(DomainType.Legal);
        consultingConfig.DomainType.Should().Be(DomainType.Consulting);

        var allConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync();
        allConfigs.Should().HaveCount(3);
    }

    [Fact]
    public async Task DomainConfigurationWithCustomDuration_ShouldStoreCorrectDuration()
    {
        // Arrange
        var domainType = DomainType.Medical;
        var name = "Extended Consultation";
        var customDuration = 90; // 1.5 hours

        // Act
        var domainConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, defaultDurationMinutes: customDuration);

        // Assert
        domainConfig.DefaultDurationMinutes.Should().Be(customDuration);

        // Verify persistence
        var retrievedConfig = await _domainConfigurationRepository.GetByIdAsync(domainConfig.Id);
        retrievedConfig!.DefaultDurationMinutes.Should().Be(customDuration);
    }

    #endregion

    #region Domain Configuration Retrieval Workflow

    [Fact]
    public async Task GetDomainConfigurationByIdWorkflow_ShouldReturnCorrectConfiguration()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(
            DomainType.Medical, "Test Medical");

        // Act
        var retrievedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);

        // Assert
        retrievedConfig.Should().NotBeNull();
        retrievedConfig!.Id.Should().Be(domainConfig.Id);
        retrievedConfig.Name.Should().Be("Test Medical");
    }

    [Fact]
    public async Task GetNonExistentDomainConfiguration_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var retrievedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(nonExistentId);

        // Assert
        retrievedConfig.Should().BeNull();
    }

    [Fact]
    public async Task GetAllDomainConfigurationsWorkflow_ShouldReturnAllActive()
    {
        // Arrange
        await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Medical 1");
        await _fixture.CreateTestDomainConfigurationAsync(DomainType.Legal, "Legal 1");
        await _fixture.CreateTestDomainConfigurationAsync(DomainType.Consulting, "Consulting 1");

        // Act
        var allConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync();

        // Assert
        allConfigs.Should().HaveCount(3);
        allConfigs.All(c => c.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllConfigurationsIncludingInactive_ShouldReturnAll()
    {
        // Arrange
        var config1 = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Active Config");
        var config2 = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Legal, "Inactive Config");

        // Deactivate config2
        await _domainConfigurationService.DeactivateDomainConfigurationAsync(config2.Id);

        // Act
        var allConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive: false);

        // Assert
        allConfigs.Should().HaveCount(2);
        allConfigs.Should().Contain(c => c.Id == config1.Id && c.IsActive);
        allConfigs.Should().Contain(c => c.Id == config2.Id && !c.IsActive);
    }

    [Fact]
    public async Task GetActiveConfigurationsOnly_ShouldFilterOutInactive()
    {
        // Arrange
        var config1 = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Active Config");
        var config2 = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Legal, "Inactive Config");

        // Deactivate config2
        await _domainConfigurationService.DeactivateDomainConfigurationAsync(config2.Id);

        // Act
        var activeConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive: true);

        // Assert
        activeConfigs.Should().HaveCount(1);
        activeConfigs.First().Id.Should().Be(config1.Id);
    }

    #endregion

    #region Domain Configuration Update Workflow

    [Fact]
    public async Task UpdateDomainConfigurationWorkflow_ShouldPersistChanges()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(
            DomainType.Medical, "Original Name", "Original Description", 30);

        // Act
        var updatedConfig = await _domainConfigurationService.UpdateDomainConfigurationAsync(
            domainConfig.Id,
            name: "Updated Name",
            description: "Updated Description",
            defaultDurationMinutes: 45);

        // Assert
        updatedConfig.Name.Should().Be("Updated Name");
        updatedConfig.Description.Should().Be("Updated Description");
        updatedConfig.DefaultDurationMinutes.Should().Be(45);
        updatedConfig.UpdatedAt.Should().NotBeNull();

        // Verify persistence
        var retrievedConfig = await _domainConfigurationRepository.GetByIdAsync(domainConfig.Id);
        retrievedConfig!.Name.Should().Be("Updated Name");
        retrievedConfig.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task PartialUpdateWorkflow_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(
            DomainType.Medical, "Original Name", "Original Description", 30);

        // Act - Update only name
        var updatedConfig = await _domainConfigurationService.UpdateDomainConfigurationAsync(
            domainConfig.Id, name: "New Name");

        // Assert
        updatedConfig.Name.Should().Be("New Name");
        updatedConfig.Description.Should().Be("Original Description"); // Unchanged
        updatedConfig.DefaultDurationMinutes.Should().Be(30); // Unchanged
    }

    [Fact]
    public async Task UpdateNonExistentConfiguration_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.UpdateDomainConfigurationAsync(nonExistentId, name: "New Name"));
    }

    #endregion

    #region Domain Configuration Activation/Deactivation Workflow

    [Fact]
    public async Task ActivateDomainConfigurationWorkflow_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test Config");

        // Deactivate first
        await _domainConfigurationService.DeactivateDomainConfigurationAsync(domainConfig.Id);
        var deactivatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        deactivatedConfig!.IsActive.Should().BeFalse();

        // Act - Reactivate
        var activateResult = await _domainConfigurationService.ActivateDomainConfigurationAsync(domainConfig.Id);

        // Assert
        activateResult.Should().BeTrue();

        var reactivatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        reactivatedConfig!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateDomainConfigurationWorkflow_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test Config");

        // Act
        var deactivateResult = await _domainConfigurationService.DeactivateDomainConfigurationAsync(domainConfig.Id);

        // Assert
        deactivateResult.Should().BeTrue();

        var deactivatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        deactivatedConfig!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateNonExistentConfiguration_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.ActivateDomainConfigurationAsync(nonExistentId));
    }

    [Fact]
    public async Task DeactivateNonExistentConfiguration_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.DeactivateDomainConfigurationAsync(nonExistentId));
    }

    #endregion

    #region Domain Configuration Deletion Workflow

    [Fact]
    public async Task DeleteDomainConfigurationWorkflow_ShouldRemoveFromDatabase()
    {
        // Arrange
        var domainConfig = await _fixture.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test Config");

        // Act
        var deleteResult = await _domainConfigurationService.DeleteDomainConfigurationAsync(domainConfig.Id);

        // Assert
        deleteResult.Should().BeTrue();

        // Verify configuration no longer exists
        var retrievedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        retrievedConfig.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNonExistentConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var deleteResult = await _domainConfigurationService.DeleteDomainConfigurationAsync(nonExistentId);

        // Assert
        deleteResult.Should().BeFalse();
    }

    #endregion

    #region Multiple Domain Configurations Workflow

    [Fact]
    public async Task MultipleMedicalConfigurationsWorkflow_ShouldSupportVariations()
    {
        // Arrange & Act
        var generalConsultation = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "General Consultation", "Standard checkup", 30);

        var specialistConsultation = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "Specialist Consultation", "With specialist", 60);

        var extendedConsultation = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "Extended Consultation", "Detailed examination", 90);

        // Assert
        var allConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync();
        var medicalConfigs = allConfigs.Where(c => c.DomainType == DomainType.Medical).ToList();

        medicalConfigs.Should().HaveCount(3);
        medicalConfigs.Should().Contain(c => c.DefaultDurationMinutes == 30);
        medicalConfigs.Should().Contain(c => c.DefaultDurationMinutes == 60);
        medicalConfigs.Should().Contain(c => c.DefaultDurationMinutes == 90);
    }

    [Fact]
    public async Task FilterByDomainTypeWorkflow_ShouldReturnCorrectTypes()
    {
        // Arrange
        await _domainConfigurationService.CreateDomainConfigurationAsync(DomainType.Medical, "Medical 1");
        await _domainConfigurationService.CreateDomainConfigurationAsync(DomainType.Medical, "Medical 2");
        await _domainConfigurationService.CreateDomainConfigurationAsync(DomainType.Legal, "Legal 1");
        await _domainConfigurationService.CreateDomainConfigurationAsync(DomainType.Consulting, "Consulting 1");

        // Act
        var allConfigs = await _domainConfigurationService.GetAllDomainConfigurationsAsync();

        // Assert
        var medicalConfigs = allConfigs.Where(c => c.DomainType == DomainType.Medical).ToList();
        var legalConfigs = allConfigs.Where(c => c.DomainType == DomainType.Legal).ToList();
        var consultingConfigs = allConfigs.Where(c => c.DomainType == DomainType.Consulting).ToList();

        medicalConfigs.Should().HaveCount(2);
        legalConfigs.Should().HaveCount(1);
        consultingConfigs.Should().HaveCount(1);
    }

    #endregion

    #region Domain Configuration Lifecycle Workflow

    [Fact]
    public async Task CompleteDomainConfigurationLifecycleWorkflow_ShouldTrackAllChanges()
    {
        // Arrange & Act - Create
        var domainConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "Initial Name", "Initial Description", 30);

        domainConfig.CreatedAt.Should().NotBeNull();
        domainConfig.UpdatedAt.Should().BeNull();

        // Act - Update
        await _domainConfigurationService.UpdateDomainConfigurationAsync(
            domainConfig.Id, name: "Updated Name");

        var updatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        updatedConfig!.UpdatedAt.Should().NotBeNull();
        updatedConfig.UpdatedAt.Should().BeAfter(updatedConfig.CreatedAt);

        // Act - Deactivate
        await _domainConfigurationService.DeactivateDomainConfigurationAsync(domainConfig.Id);

        var deactivatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        deactivatedConfig!.IsActive.Should().BeFalse();

        // Act - Reactivate
        await _domainConfigurationService.ActivateDomainConfigurationAsync(domainConfig.Id);

        var reactivatedConfig = await _domainConfigurationService.GetDomainConfigurationByIdAsync(domainConfig.Id);
        reactivatedConfig!.IsActive.Should().BeTrue();
    }

    #endregion
}