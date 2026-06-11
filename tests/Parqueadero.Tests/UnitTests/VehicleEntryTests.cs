using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;

namespace Parqueadero.Tests.UnitTests;

public class VehicleEntryTests
{
    [Fact]
    public void Constructor_ShouldSetProperties_WhenCalledWithValidArguments()
    {
        // Arrange
        var plate = "ABC123";
        var vehicleTypeId = 1; // Carro
        var entryTime = new DateTime(2026, 6, 9, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var entry = new VehicleEntry(vehicleTypeId, plate, entryTime);

        // Assert
        entry.Id.Should().NotBeEmpty();
        entry.Plate.Should().Be("ABC123");
        entry.VehicleTypeId.Should().Be(1);
        entry.EntryTime.Should().Be(entryTime);
        entry.ExitTime.Should().BeNull();
        entry.TotalMinutes.Should().BeNull();
        entry.Fee.Should().BeNull();
        entry.EmailSent.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldConvertPlateToUpper()
    {
        // Arrange
        var plate = "abc123";
        var vehicleTypeId = 2; // Moto
        var entryTime = DateTime.UtcNow;

        // Act
        var entry = new VehicleEntry(vehicleTypeId, plate, entryTime);

        // Assert
        entry.Plate.Should().Be("ABC123");
    }

    [Fact]
    public void Exit_ShouldSetExitTimeAndCalculateFee_WhenVehicleHasNotExited()
    {
        // Arrange
        var entryTime = new DateTime(2026, 6, 9, 10, 0, 0, DateTimeKind.Utc);
        var exitTime = new DateTime(2026, 6, 9, 10, 30, 0, DateTimeKind.Utc);
        var entry = new VehicleEntry(1, "ABC123", entryTime); // Carro

        // Act
        var result = entry.Exit(exitTime);

        // Assert
        entry.ExitTime.Should().Be(exitTime);
        entry.TotalMinutes.Should().Be(30);
        entry.Fee.Should().Be(1500m); // 30 * 50
        result.TotalMinutes.Should().Be(30);
        result.Fee.Should().Be(1500m);
    }

    [Fact]
    public void Exit_ShouldThrowConflictException_WhenVehicleAlreadyExited()
    {
        // Arrange
        var entryTime = new DateTime(2026, 6, 9, 10, 0, 0, DateTimeKind.Utc);
        var exitTime = new DateTime(2026, 6, 9, 10, 30, 0, DateTimeKind.Utc);
        var entry = new VehicleEntry(1, "ABC123", entryTime); // Carro
        entry.Exit(exitTime);

        // Act
        var act = () => entry.Exit(new DateTime(2026, 6, 9, 11, 0, 0, DateTimeKind.Utc));

        // Assert
        act.Should().Throw<ConflictException>().WithMessage("*ya no se encuentra*");
    }

    [Fact]
    public void Exit_ShouldRoundUpTotalMinutes_WhenPartialMinutes()
    {
        // Arrange
        var entryTime = new DateTime(2026, 6, 9, 10, 0, 0, DateTimeKind.Utc);
        var exitTime = new DateTime(2026, 6, 9, 10, 30, 30, DateTimeKind.Utc); // 30.5 min
        var entry = new VehicleEntry(2, "XYZ789", entryTime); // Moto

        // Act
        var result = entry.Exit(exitTime);

        // Assert
        entry.TotalMinutes.Should().Be(31); // Ceiling of 30.5
        entry.Fee.Should().Be(1550m); // 31 * 50
        result.TotalMinutes.Should().Be(31);
        result.Fee.Should().Be(1550m);
    }

    [Fact]
    public void MarkEmailSent_ShouldSetEmailSentToTrue()
    {
        // Arrange
        var entry = new VehicleEntry(1, "ABC123", DateTime.UtcNow); // Carro

        // Act
        entry.MarkEmailSent();

        // Assert
        entry.EmailSent.Should().BeTrue();
    }
}
