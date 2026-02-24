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
/// Comprehensive unit tests for DomainConfigurationService covering all scenarios and functionality
/// Module: Domain Configuration Module (1.4)
/// </summary>
public class DomainConfigurationServiceTests
{
    private readonly Mock<IDomainConfigurationRepository> _mockDomainConfigurationRepository;
    private readonly DomainConfigurationService _domainConfigurationService;

    public DomainConfigurationServiceTests()
    {
        _mockDomainConfigurationRepository = new Mock<IDomainConfigurationRepository>();
        _domainConfigurationService = new DomainConfigurationService(_mockDomainConfigurationRepository.Object);
    }

    #region CreateDomainConfigurationAsync Tests

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var domainType = DomainType.Medical;
        const string name = "General Practice";
        const string description = "General medical consultations";
        const int defaultDurationMinutes = 60;

        var createdConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            Description = description,
            DefaultDurationMinutes = defaultDurationMinutes,
            IsActive = true
        };

        _mockDomainConfigurationRepository.Setup(x => x.CreateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(createdConfiguration);

        // Act
        var result = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, description, defaultDurationMinutes);

        // Assert
        result.Should().NotBeNull();
        result.DomainType.Should().Be(domainType);
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.DefaultDurationMinutes.Should().Be(defaultDurationMinutes);
        result.IsActive.Should().BeTrue();
        _mockDomainConfigurationRepository.Verify(x => x.CreateAsync(It.IsAny<DomainConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var domainType = DomainType.Medical;
        const string name = "";

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.CreateDomainConfigurationAsync(domainType, name));

        // Assert
        exception.ParamName.Should().Be(nameof(name));
        exception.Message.Should().Contain("Name is required");
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var domainType = DomainType.Medical;
        const string name = "   ";

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.CreateDomainConfigurationAsync(domainType, name));

        // Assert
        exception.ParamName.Should().Be(nameof(name));
        exception.Message.Should().Contain("Name is required");
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var domainType = DomainType.Medical;

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.CreateDomainConfigurationAsync(domainType, null!));

        // Assert
        exception.ParamName.Should().Be(nameof(name));
        exception.Message.Should().Contain("Name is required");
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithLegalDomain_ShouldCreateWithCorrectType()
    {
        // Arrange
        var domainType = DomainType.Legal;
        const string name = "Corporate Law";
        const int defaultDurationMinutes = 90;

        var createdConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = defaultDurationMinutes,
            IsActive = true
        };

        _mockDomainConfigurationRepository.Setup(x => x.CreateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(createdConfiguration);

        // Act
        var result = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, defaultDurationMinutes: defaultDurationMinutes);

        // Assert
        result.DomainType.Should().Be(DomainType.Legal);
        result.DefaultDurationMinutes.Should().Be(90);
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithConsultingDomain_ShouldCreateWithCorrectType()
    {
        // Arrange
        var domainType = DomainType.Consulting;
        const string name = "Business Consulting";
        const int defaultDurationMinutes = 45;

        var createdConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = defaultDurationMinutes,
            IsActive = true
        };

        _mockDomainConfigurationRepository.Setup(x => x.CreateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(createdConfiguration);

        // Act
        var result = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, defaultDurationMinutes: defaultDurationMinutes);

        // Assert
        result.DomainType.Should().Be(DomainType.Consulting);
        result.DefaultDurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithCustomDuration_ShouldSetCorrectDuration()
    {
        // Arrange
        var domainType = DomainType.Medical;
        const string name = "Specialist Consultation";
        const int customDurationMinutes = 120;

        var createdConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = customDurationMinutes,
            IsActive = true
        };

        _mockDomainConfigurationRepository.Setup(x => x.CreateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(createdConfiguration);

        // Act
        var result = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name, defaultDurationMinutes: customDurationMinutes);

        // Assert
        result.DefaultDurationMinutes.Should().Be(customDurationMinutes);
    }

    [Fact]
    public async Task CreateDomainConfigurationAsync_WithoutDescription_ShouldCreateSuccessfully()
    {
        // Arrange
        var domainType = DomainType.Medical;
        const string name = "Quick Consultation";

        var createdConfiguration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = 30,
            IsActive = true
        };

        _mockDomainConfigurationRepository.Setup(x => x.CreateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(createdConfiguration);

        // Act
        var result = await _domainConfigurationService.CreateDomainConfigurationAsync(
            domainType, name);

        // Assert
        result.Description.Should().BeNull();
    }

    #endregion

    #region GetDomainConfigurationByIdAsync Tests

    [Fact]
    public async Task GetDomainConfigurationByIdAsync_WithExistingId_ShouldReturnConfiguration()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var configuration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "General Practice",
            DomainType = DomainType.Medical
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(configuration);

        // Act
        var result = await _domainConfigurationService.GetDomainConfigurationByIdAsync(configurationId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(configurationId);
        result.Name.Should().Be("General Practice");
        _mockDomainConfigurationRepository.Verify(x => x.GetByIdAsync(configurationId), Times.Once);
    }

    [Fact]
    public async Task GetDomainConfigurationByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync((DomainConfiguration?)null);

        // Act
        var result = await _domainConfigurationService.GetDomainConfigurationByIdAsync(configurationId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllDomainConfigurationsAsync Tests

    [Fact]
    public async Task GetAllDomainConfigurationsAsync_WithOnlyActiveTrue_ShouldReturnOnlyActive()
    {
        // Arrange
        var configurations = new List<DomainConfiguration>
        {
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Active 1", IsActive = true },
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Active 2", IsActive = true },
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false }
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(configurations.Where(c => c.IsActive));

        // Act
        var result = await _domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive: true);

        // Assert
        result.Should().HaveCount(2);
        result.All(c => c.IsActive).Should().BeTrue();
        _mockDomainConfigurationRepository.Verify(x => x.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetAllDomainConfigurationsAsync_WithOnlyActiveFalse_ShouldReturnAll()
    {
        // Arrange
        var configurations = new List<DomainConfiguration>
        {
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Active 1", IsActive = true },
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false }
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetAllAsync(false))
            .ReturnsAsync(configurations);

        // Act
        var result = await _domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive: false);

        // Assert
        result.Should().HaveCount(2);
        _mockDomainConfigurationRepository.Verify(x => x.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetAllDomainConfigurationsAsync_WithDefaultParameter_ShouldReturnOnlyActive()
    {
        // Arrange
        var configurations = new List<DomainConfiguration>
        {
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Active 1", IsActive = true }
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetAllAsync(true))
            .ReturnsAsync(configurations);

        // Act
        var result = await _domainConfigurationService.GetAllDomainConfigurationsAsync();

        // Assert
        result.Should().HaveCount(1);
        _mockDomainConfigurationRepository.Verify(x => x.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetAllDomainConfigurationsAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockDomainConfigurationRepository.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<DomainConfiguration>());

        // Act
        var result = await _domainConfigurationService.GetAllDomainConfigurationsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllDomainConfigurationsAsync_ShouldIncludeAllDomainTypes()
    {
        // Arrange
        var configurations = new List<DomainConfiguration>
        {
            new DomainConfiguration { Id = Guid.NewGuid(), DomainType = DomainType.Medical, IsActive = true },
            new DomainConfiguration { Id = Guid.NewGuid(), DomainType = DomainType.Legal, IsActive = true },
            new DomainConfiguration { Id = Guid.NewGuid(), DomainType = DomainType.Consulting, IsActive = true }
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(configurations);

        // Act
        var result = await _domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive: false);

        // Assert
        result.Should().Contain(c => c.DomainType == DomainType.Medical);
        result.Should().Contain(c => c.DomainType == DomainType.Legal);
        result.Should().Contain(c => c.DomainType == DomainType.Consulting);
    }

    #endregion

    #region UpdateDomainConfigurationAsync Tests

    [Fact]
    public async Task UpdateDomainConfigurationAsync_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        const string newName = "Updated Name";
        const string newDescription = "Updated description";
        const int newDurationMinutes = 90;

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            Description = "Original description",
            DefaultDurationMinutes = 60,
            IsActive = true
        };

        var updatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = newName,
            Description = newDescription,
            DefaultDurationMinutes = newDurationMinutes,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(updatedConfiguration);

        // Act
        var result = await _domainConfigurationService.UpdateDomainConfigurationAsync(
            configurationId, newName, newDescription, newDurationMinutes);

        // Assert
        result.Name.Should().Be(newName);
        result.Description.Should().Be(newDescription);
        result.DefaultDurationMinutes.Should().Be(newDurationMinutes);
        result.UpdatedAt.Should().NotBeNull();
        _mockDomainConfigurationRepository.Verify(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDomainConfigurationAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        const string newName = "Updated Name";

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            Description = "Original description",
            DefaultDurationMinutes = 60
        };

        var updatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = newName,
            Description = "Original description",
            DefaultDurationMinutes = 60
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(updatedConfiguration);

        // Act
        var result = await _domainConfigurationService.UpdateDomainConfigurationAsync(configurationId, newName);

        // Assert
        result.Name.Should().Be(newName);
        result.Description.Should().Be("Original description");
        result.DefaultDurationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task UpdateDomainConfigurationAsync_WithNonExistentConfiguration_ShouldThrowArgumentException()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync((DomainConfiguration?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.UpdateDomainConfigurationAsync(configurationId));

        // Assert
        exception.ParamName.Should().Be(nameof(configurationId));
        exception.Message.Should().Contain("Domain configuration not found");
    }

    [Fact]
    public async Task UpdateDomainConfigurationAsync_WithOnlyDescription_ShouldUpdateDescriptionOnly()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        const string newDescription = "Updated description only";

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            Description = "Original description",
            DefaultDurationMinutes = 60
        };

        var updatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            Description = newDescription,
            DefaultDurationMinutes = 60
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(updatedConfiguration);

        // Act
        var result = await _domainConfigurationService.UpdateDomainConfigurationAsync(
            configurationId, description: newDescription);

        // Assert
        result.Description.Should().Be(newDescription);
        result.Name.Should().Be("Original Name");
    }

    [Fact]
    public async Task UpdateDomainConfigurationAsync_WithOnlyDuration_ShouldUpdateDurationOnly()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        const int newDurationMinutes = 120;

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            DefaultDurationMinutes = 60
        };

        var updatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Original Name",
            DefaultDurationMinutes = newDurationMinutes
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(updatedConfiguration);

        // Act
        var result = await _domainConfigurationService.UpdateDomainConfigurationAsync(
            configurationId, defaultDurationMinutes: newDurationMinutes);

        // Assert
        result.DefaultDurationMinutes.Should().Be(newDurationMinutes);
        result.Name.Should().Be("Original Name");
    }

    #endregion

    #region ActivateDomainConfigurationAsync Tests

    [Fact]
    public async Task ActivateDomainConfigurationAsync_WithInactiveConfiguration_ShouldActivateSuccessfully()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = false
        };

        var activatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(activatedConfiguration);

        // Act
        var result = await _domainConfigurationService.ActivateDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeTrue();
        _mockDomainConfigurationRepository.Verify(x => x.UpdateAsync(
            It.Is<DomainConfiguration>(c => c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task ActivateDomainConfigurationAsync_WithActiveConfiguration_ShouldRemainActive()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = true
        };

        var activatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(activatedConfiguration);

        // Act
        var result = await _domainConfigurationService.ActivateDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateDomainConfigurationAsync_WithNonExistentConfiguration_ShouldThrowArgumentException()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync((DomainConfiguration?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.ActivateDomainConfigurationAsync(configurationId));

        // Assert
        exception.ParamName.Should().Be(nameof(configurationId));
        exception.Message.Should().Contain("Domain configuration not found");
    }

    #endregion

    #region DeactivateDomainConfigurationAsync Tests

    [Fact]
    public async Task DeactivateDomainConfigurationAsync_WithActiveConfiguration_ShouldDeactivateSuccessfully()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = true
        };

        var deactivatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = false,
            UpdatedAt = DateTime.UtcNow
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(deactivatedConfiguration);

        // Act
        var result = await _domainConfigurationService.DeactivateDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeTrue();
        _mockDomainConfigurationRepository.Verify(x => x.UpdateAsync(
            It.Is<DomainConfiguration>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task DeactivateDomainConfigurationAsync_WithInactiveConfiguration_ShouldRemainInactive()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        var existingConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = false
        };

        var deactivatedConfiguration = new DomainConfiguration
        {
            Id = configurationId,
            Name = "Test Configuration",
            IsActive = false,
            UpdatedAt = DateTime.UtcNow
        };

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync(existingConfiguration);
        _mockDomainConfigurationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainConfiguration>()))
            .ReturnsAsync(deactivatedConfiguration);

        // Act
        var result = await _domainConfigurationService.DeactivateDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateDomainConfigurationAsync_WithNonExistentConfiguration_ShouldThrowArgumentException()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.GetByIdAsync(configurationId))
            .ReturnsAsync((DomainConfiguration?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _domainConfigurationService.DeactivateDomainConfigurationAsync(configurationId));

        // Assert
        exception.ParamName.Should().Be(nameof(configurationId));
        exception.Message.Should().Contain("Domain configuration not found");
    }

    #endregion

    #region DeleteDomainConfigurationAsync Tests

    [Fact]
    public async Task DeleteDomainConfigurationAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.DeleteAsync(configurationId))
            .ReturnsAsync(true);

        // Act
        var result = await _domainConfigurationService.DeleteDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeTrue();
        _mockDomainConfigurationRepository.Verify(x => x.DeleteAsync(configurationId), Times.Once);
    }

    [Fact]
    public async Task DeleteDomainConfigurationAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var configurationId = Guid.NewGuid();

        _mockDomainConfigurationRepository.Setup(x => x.DeleteAsync(configurationId))
            .ReturnsAsync(false);

        // Act
        var result = await _domainConfigurationService.DeleteDomainConfigurationAsync(configurationId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}