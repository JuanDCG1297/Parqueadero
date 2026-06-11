using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;

namespace Parqueadero.Tests.UnitTests;

public class ParkingServiceTests
{
    private readonly Mock<IVehicleRepository> _repoMock = new();
    private readonly Mock<IVehicleTypeRepository> _typeRepoMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<IValidator<EntryRequest>> _validatorMock = new();
    private readonly Mock<ILogger<ParkingService>> _loggerMock = new();
    private readonly ParkingService _sut;

    public ParkingServiceTests()
    {
        _typeRepoMock.Setup(r => r.GetByNameAsync("Carro", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VehicleType { Id = 1, Name = "Carro" });
        _typeRepoMock.Setup(r => r.GetByNameAsync("Moto", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VehicleType { Id = 2, Name = "Moto" });

        _sut = new ParkingService(
            _repoMock.Object,
            _typeRepoMock.Object,
            _emailMock.Object,
            _validatorMock.Object,
            _loggerMock.Object
        );
    }

    // ─── RegisterEntryAsync ─────────────────────────────────────

    [Fact]
    public async Task RegisterEntryAsync_ShouldReturnEntryResponse_WhenRequestIsValid()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", null);
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _repoMock.Setup(r => r.ExistsActivePlateAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var response = await _sut.RegisterEntryAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Plate.Should().Be("ABC123");
        response.VehicleType.Should().Be("Carro");
        response.Id.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<VehicleEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterEntryAsync_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var request = new EntryRequest("Carro", "", null);
        var failures = new List<ValidationFailure> { new("Plate", "Plate is required.") };
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var act = () => _sut.RegisterEntryAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<VehicleEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterEntryAsync_ShouldThrowConflictException_WhenPlateAlreadyParked()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", null);
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _repoMock.Setup(r => r.ExistsActivePlateAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.RegisterEntryAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ya está estacionado*");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<VehicleEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── RegisterExitAsync ──────────────────────────────────────

    [Fact]
    public async Task RegisterExitAsync_ShouldReturnExitResponse_WhenVehicleExists()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var entry = new VehicleEntry(1, "ABC123", DateTime.UtcNow.AddHours(-2)); // Carro
        // Use reflection to set the Id and VehicleType navigation for testing
        typeof(VehicleEntry).GetProperty(nameof(VehicleEntry.Id))!.SetValue(entry, entryId);
        typeof(VehicleEntry).GetProperty(nameof(VehicleEntry.VehicleType))!.SetValue(entry, new VehicleType { Id = 1, Name = "Carro" });

        _repoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _emailMock.Setup(e => e.SendExitNotificationAsync(entry, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _sut.RegisterExitAsync(entryId);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(entryId);
        response.Plate.Should().Be("ABC123");
        response.VehicleType.Should().Be("Carro");
        response.TotalMinutes.Should().BeGreaterThan(0);
        response.Fee.Should().BeGreaterThan(0);
        _repoMock.Verify(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
        _emailMock.Verify(e => e.SendExitNotificationAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterExitAsync_ShouldThrowNotFoundException_WhenVehicleNotFound()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehicleEntry?)null);

        // Act
        var act = () => _sut.RegisterExitAsync(entryId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VehicleEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── GetByIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetByPlateAsync_ShouldReturnVehicleResponse_WhenVehicleExists()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var entry = new VehicleEntry(2, "XYZ789", DateTime.UtcNow.AddHours(-1)); // Moto
        typeof(VehicleEntry).GetProperty(nameof(VehicleEntry.Id))!.SetValue(entry, entryId);
        typeof(VehicleEntry).GetProperty(nameof(VehicleEntry.VehicleType))!.SetValue(entry, new VehicleType { Id = 2, Name = "Moto" });

        _repoMock.Setup(r => r.GetByPlateAsync("XYZ789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var response = await _sut.GetByPlateAsync("XYZ789");

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(entryId);
        response.Plate.Should().Be("XYZ789");
        response.VehicleType.Should().Be("Moto");
        response.ExitTime.Should().BeNull();
        response.TotalMinutes.Should().BeNull();
        response.Fee.Should().BeNull();
    }

    [Fact]
    public async Task GetByPlateAsync_ShouldThrowNotFoundException_WhenVehicleNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPlateAsync("NONEXIST", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehicleEntry?)null);

        // Act
        var act = () => _sut.GetByPlateAsync("NONEXIST");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
