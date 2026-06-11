using Application.DTOs;
using Application.Validators;
using FluentAssertions;
using FluentValidation;

namespace Parqueadero.Tests.UnitTests;

public class EntryRequestValidatorTests
{
    private readonly EntryRequestValidator _sut = new();

    [Fact]
    public void ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldFail_WhenPlateIsEmpty()
    {
        // Arrange
        var request = new EntryRequest("Carro", "", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plate" && e.ErrorMessage.Contains("requerida"));
    }

    [Fact]
    public void ShouldFail_WhenPlateExceedsMaxLength()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABCDEFGHIJK", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plate" && e.ErrorMessage.Contains("10"));
    }

    [Fact]
    public void ShouldFail_WhenPlateContainsSpecialChars()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC-123", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plate" && e.ErrorMessage.Contains("alfanumérica"));
    }

    [Fact]
    public void ShouldFail_WhenPlateIsNotUppercase()
    {
        // Arrange
        var request = new EntryRequest("Carro", "abc123", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Plate" && e.ErrorMessage.Contains("mayúsculas"));
    }

    [Fact]
    public void ShouldFail_WhenVehicleTypeIsInvalid()
    {
        // Arrange
        var request = new EntryRequest("Camion", "ABC123", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VehicleType" && e.ErrorMessage.Contains("Carro"));
    }

    [Fact]
    public void ShouldPass_WhenVehicleTypeIsMoto()
    {
        // Arrange
        var request = new EntryRequest("Moto", "XYZ789", DateTime.UtcNow);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldFail_WhenEntryTimeIsInFuture()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", DateTime.UtcNow.AddHours(1));

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EntryTime" && e.ErrorMessage.Contains("futuro"));
    }

    [Fact]
    public void ShouldPass_WhenEntryTimeIsNull()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", null);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
