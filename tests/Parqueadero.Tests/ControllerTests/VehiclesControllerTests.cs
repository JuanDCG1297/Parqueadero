using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Web.Api.Controllers;

namespace Parqueadero.Tests.ControllerTests;

public class VehiclesControllerTests
{
    private readonly Mock<IParkingService> _serviceMock = new();
    private readonly VehiclesController _sut;

    public VehiclesControllerTests()
    {
        _sut = new VehiclesController(_serviceMock.Object);
    }

    [Fact]
    public async Task RegisterEntry_ShouldReturn201Created_WhenSuccessful()
    {
        // Arrange
        var request = new EntryRequest("Carro", "ABC123", null);
        var response = new EntryResponse(Guid.NewGuid(), "Carro", "ABC123", DateTime.UtcNow);

        _serviceMock.Setup(s => s.RegisterEntryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.RegisterEntry(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(VehiclesController.GetById));
        createdResult.RouteValues!["id"].Should().Be(response.Id);
        createdResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task RegisterExit_ShouldReturn200Ok_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new ExitResponse(id, "ABC123", "Carro",
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, 120, 6000m, false);

        _serviceMock.Setup(s => s.RegisterExitAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.RegisterExit(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task RegisterExit_ShouldReturn404_WhenNotFoundExceptionThrown()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RegisterExitAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Not found"));

        // Act
        var act = () => _sut.RegisterExit(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_ShouldReturn200Ok_WhenVehicleExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var response = new VehicleResponse(id, "ABC123", "Carro",
            DateTime.UtcNow, null, null, null, false);

        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetById(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenNotFoundExceptionThrown()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Not found"));

        // Act
        var act = () => _sut.GetById(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
