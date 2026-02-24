using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Appointment.UnitTests.Services;

/// <summary>
/// Comprehensive unit tests for PreOrderDataService covering all scenarios and functionality
/// Module: Pre-Order Data Collection Module (1.5)
/// </summary>
public class PreOrderDataServiceTests
{
    private readonly Mock<IPreOrderDataRepository> _mockPreOrderDataRepository;
    private readonly PreOrderDataService _preOrderDataService;

    public PreOrderDataServiceTests()
    {
        _mockPreOrderDataRepository = new Mock<IPreOrderDataRepository>();
        _preOrderDataService = new PreOrderDataService(_mockPreOrderDataRepository.Object);
    }

    #region CreatePreOrderDataAsync Tests

    [Fact]
    public async Task CreatePreOrderDataAsync_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache and fever" },
            { "duration", "3 days" },
            { "medications", "None" }
        };

        var createdPreOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = dataFields,
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.CreateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(createdPreOrderData);

        // Act
        var result = await _preOrderDataService.CreatePreOrderDataAsync(orderId, clientId, dataFields);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.ClientId.Should().Be(clientId);
        result.DataFields.Should().HaveCount(3);
        result.IsCompleted.Should().BeFalse();
        _mockPreOrderDataRepository.Verify(x => x.CreateAsync(It.IsAny<PreOrderData>()), Times.Once);
    }

    [Fact]
    public async Task CreatePreOrderDataAsync_WithEmptyDataFields_ShouldCreateSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var emptyDataFields = new Dictionary<string, string>();

        var createdPreOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = emptyDataFields,
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.CreateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(createdPreOrderData);

        // Act
        var result = await _preOrderDataService.CreatePreOrderDataAsync(orderId, clientId, emptyDataFields);

        // Assert
        result.DataFields.Should().BeEmpty();
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePreOrderDataAsync_WithSingleDataField_ShouldCreateSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var dataFields = new Dictionary<string, string>
        {
            { "reason", "Annual checkup" }
        };

        var createdPreOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = dataFields,
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.CreateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(createdPreOrderData);

        // Act
        var result = await _preOrderDataService.CreatePreOrderDataAsync(orderId, clientId, dataFields);

        // Assert
        result.DataFields.Should().HaveCount(1);
        result.DataFields.Should().ContainKey("reason");
        result.DataFields["reason"].Should().Be("Annual checkup");
    }

    [Fact]
    public async Task CreatePreOrderDataAsync_WithMultipleDataFields_ShouldPreserveAllFields()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var dataFields = new Dictionary<string, string>
        {
            { "name", "John Doe" },
            { "age", "35" },
            { "phone", "123-456-7890" },
            { "email", "john@example.com" },
            { "address", "123 Main St" }
        };

        var createdPreOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = dataFields,
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.CreateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(createdPreOrderData);

        // Act
        var result = await _preOrderDataService.CreatePreOrderDataAsync(orderId, clientId, dataFields);

        // Assert
        result.DataFields.Should().HaveCount(5);
        result.DataFields.Should().ContainKey("name");
        result.DataFields.Should().ContainKey("age");
        result.DataFields.Should().ContainKey("phone");
        result.DataFields.Should().ContainKey("email");
        result.DataFields.Should().ContainKey("address");
    }

    [Fact]
    public async Task CreatePreOrderDataAsync_WithSpecialCharactersInValues_ShouldCreateSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var dataFields = new Dictionary<string, string>
        {
            { "description", "Patient has symptoms: fever, headache, nausea" },
            { "allergies", "Penicillin, sulfa drugs" }
        };

        var createdPreOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = dataFields,
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.CreateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(createdPreOrderData);

        // Act
        var result = await _preOrderDataService.CreatePreOrderDataAsync(orderId, clientId, dataFields);

        // Assert
        result.DataFields["description"].Should().Contain(",");
        result.DataFields["allergies"].Should().Contain(",");
    }

    #endregion

    #region GetPreOrderDataByOrderIdAsync Tests

    [Fact]
    public async Task GetPreOrderDataByOrderIdAsync_WithExistingOrderId_ShouldReturnPreOrderData()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            },
            IsCompleted = false
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.GetPreOrderDataByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.ClientId.Should().Be(clientId);
        _mockPreOrderDataRepository.Verify(x => x.GetByOrderIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetPreOrderDataByOrderIdAsync_WithNonExistentOrderId_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockPreOrderDataRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((PreOrderData?)null);

        // Act
        var result = await _preOrderDataService.GetPreOrderDataByOrderIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdatePreOrderDataAsync Tests

    [Fact]
    public async Task UpdatePreOrderDataAsync_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            },
            IsCompleted = false
        };

        var newDataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache and fever" },
            { "duration", "2 days" }
        };

        var updatedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = existingPreOrderData.OrderId,
            ClientId = existingPreOrderData.ClientId,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache and fever" },
                { "duration", "2 days" }
            },
            IsCompleted = false,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(updatedPreOrderData);

        // Act
        var result = await _preOrderDataService.UpdatePreOrderDataAsync(preOrderDataId, newDataFields);

        // Assert
        result.DataFields.Should().HaveCount(2);
        result.DataFields["symptoms"].Should().Be("Headache and fever");
        result.DataFields["duration"].Should().Be("2 days");
        result.UpdatedAt.Should().NotBeNull();
        _mockPreOrderDataRepository.Verify(x => x.UpdateAsync(It.IsAny<PreOrderData>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreOrderDataAsync_WithNewField_ShouldAddField()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            },
            IsCompleted = false
        };

        var newDataFields = new Dictionary<string, string>
        {
            { "duration", "3 days" }
        };

        var updatedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" },
                { "duration", "3 days" }
            }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(updatedPreOrderData);

        // Act
        var result = await _preOrderDataService.UpdatePreOrderDataAsync(preOrderDataId, newDataFields);

        // Assert
        result.DataFields.Should().HaveCount(2);
        result.DataFields.Should().ContainKey("symptoms");
        result.DataFields.Should().ContainKey("duration");
    }

    [Fact]
    public async Task UpdatePreOrderDataAsync_WithExistingField_ShouldUpdateFieldValue()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            },
            IsCompleted = false
        };

        var newDataFields = new Dictionary<string, string>
        {
            { "symptoms", "Migraine" }
        };

        var updatedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Migraine" }
            }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(updatedPreOrderData);

        // Act
        var result = await _preOrderDataService.UpdatePreOrderDataAsync(preOrderDataId, newDataFields);

        // Assert
        result.DataFields["symptoms"].Should().Be("Migraine");
    }

    [Fact]
    public async Task UpdatePreOrderDataAsync_WithNonExistentPreOrderData_ShouldThrowArgumentException()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();
        var newDataFields = new Dictionary<string, string>();

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync((PreOrderData?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _preOrderDataService.UpdatePreOrderDataAsync(preOrderDataId, newDataFields));

        // Assert
        exception.ParamName.Should().Be(nameof(preOrderDataId));
        exception.Message.Should().Contain("Pre-order data not found");
    }

    [Fact]
    public async Task UpdatePreOrderDataAsync_WithEmptyUpdateFields_ShouldNotModifyData()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            },
            IsCompleted = false
        };

        var emptyDataFields = new Dictionary<string, string>();

        var updatedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" }
            }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(updatedPreOrderData);

        // Act
        var result = await _preOrderDataService.UpdatePreOrderDataAsync(preOrderDataId, emptyDataFields);

        // Assert
        result.DataFields.Should().HaveCount(1);
        result.DataFields["symptoms"].Should().Be("Headache");
    }

    #endregion

    #region MarkAsCompletedAsync Tests

    [Fact]
    public async Task MarkAsCompletedAsync_WithValidPreOrderData_ShouldMarkSuccessfully()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>(),
            IsCompleted = false
        };

        var completedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = existingPreOrderData.OrderId,
            ClientId = existingPreOrderData.ClientId,
            DataFields = existingPreOrderData.DataFields,
            IsCompleted = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(completedPreOrderData);

        // Act
        var result = await _preOrderDataService.MarkAsCompletedAsync(preOrderDataId);

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.UpdatedAt.Should().NotBeNull();
        _mockPreOrderDataRepository.Verify(x => x.UpdateAsync(It.IsAny<PreOrderData>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WithAlreadyCompleted_ShouldRemainCompleted()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var existingPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>(),
            IsCompleted = true
        };

        var completedPreOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            IsCompleted = true,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(existingPreOrderData);
        _mockPreOrderDataRepository.Setup(x => x.UpdateAsync(It.IsAny<PreOrderData>()))
            .ReturnsAsync(completedPreOrderData);

        // Act
        var result = await _preOrderDataService.MarkAsCompletedAsync(preOrderDataId);

        // Assert
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WithNonExistentPreOrderData_ShouldThrowArgumentException()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync((PreOrderData?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _preOrderDataService.MarkAsCompletedAsync(preOrderDataId));

        // Assert
        exception.ParamName.Should().Be(nameof(preOrderDataId));
        exception.Message.Should().Contain("Pre-order data not found");
    }

    #endregion

    #region ValidatePreOrderDataAsync Tests

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithAllRequiredFieldsPresent_ShouldReturnTrue()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "age", "35" },
                { "phone", "123-456-7890" }
            },
            IsCompleted = false
        };

        var requiredFields = new Dictionary<string, string>
        {
            { "name", "" },
            { "age", "" },
            { "phone", "" }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithMissingRequiredField_ShouldReturnFalse()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "age", "35" }
                // "phone" is missing
            },
            IsCompleted = false
        };

        var requiredFields = new Dictionary<string, string>
        {
            { "name", "" },
            { "age", "" },
            { "phone", "" }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithEmptyRequiredFieldValue_ShouldReturnFalse()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "age", "" }, // Empty value
                { "phone", "123-456-7890" }
            },
            IsCompleted = false
        };

        var requiredFields = new Dictionary<string, string>
        {
            { "name", "" },
            { "age", "" },
            { "phone", "" }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithWhitespaceRequiredFieldValue_ShouldReturnFalse()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "age", "   " }, // Whitespace only
                { "phone", "123-456-7890" }
            },
            IsCompleted = false
        };

        var requiredFields = new Dictionary<string, string>
        {
            { "name", "" },
            { "age", "" },
            { "phone", "" }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithNonExistentPreOrderData_ShouldReturnFalse()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();
        var requiredFields = new Dictionary<string, string>();

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync((PreOrderData?)null);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithEmptyRequiredFieldsList_ShouldReturnTrue()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>(),
            IsCompleted = false
        };

        var emptyRequiredFields = new Dictionary<string, string>();

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, emptyRequiredFields);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePreOrderDataAsync_WithAdditionalFieldsBeyondRequired_ShouldReturnTrue()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        var preOrderData = new PreOrderData
        {
            Id = preOrderDataId,
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            DataFields = new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "age", "35" },
                { "phone", "123-456-7890" },
                { "email", "john@example.com" }, // Additional field
                { "address", "123 Main St" } // Additional field
            },
            IsCompleted = false
        };

        var requiredFields = new Dictionary<string, string>
        {
            { "name", "" },
            { "age", "" },
            { "phone", "" }
        };

        _mockPreOrderDataRepository.Setup(x => x.GetByIdAsync(preOrderDataId))
            .ReturnsAsync(preOrderData);

        // Act
        var result = await _preOrderDataService.ValidatePreOrderDataAsync(preOrderDataId, requiredFields);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region DeletePreOrderDataAsync Tests

    [Fact]
    public async Task DeletePreOrderDataAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        _mockPreOrderDataRepository.Setup(x => x.DeleteAsync(preOrderDataId))
            .ReturnsAsync(true);

        // Act
        var result = await _preOrderDataService.DeletePreOrderDataAsync(preOrderDataId);

        // Assert
        result.Should().BeTrue();
        _mockPreOrderDataRepository.Verify(x => x.DeleteAsync(preOrderDataId), Times.Once);
    }

    [Fact]
    public async Task DeletePreOrderDataAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var preOrderDataId = Guid.NewGuid();

        _mockPreOrderDataRepository.Setup(x => x.DeleteAsync(preOrderDataId))
            .ReturnsAsync(false);

        // Act
        var result = await _preOrderDataService.DeletePreOrderDataAsync(preOrderDataId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}