using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Parqueadero.Tests.IntegrationTests;

public class VehicleRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistVehicleEntry()
    {
        // Arrange
        var db = CreateDbContext("Test_Add_Persist");
        var repo = new VehicleRepository(db);
        var entry = new VehicleEntry("Carro", "ABC123", DateTime.UtcNow);

        // Act
        await repo.AddAsync(entry);

        // Assert
        var saved = await db.VehicleEntries.FindAsync(entry.Id);
        saved.Should().NotBeNull();
        saved!.Plate.Should().Be("ABC123");
        saved.VehicleType.Should().Be("Carro");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var db = CreateDbContext("Test_GetById_NotFound");
        var repo = new VehicleRepository(db);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntry_WhenExists()
    {
        // Arrange
        var db = CreateDbContext("Test_GetById_Found");
        var repo = new VehicleRepository(db);
        var entry = new VehicleEntry("Moto", "XYZ789", DateTime.UtcNow);
        await repo.AddAsync(entry);

        // Act
        var result = await repo.GetByIdAsync(entry.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entry.Id);
        result.Plate.Should().Be("XYZ789");
    }

    [Fact]
    public async Task ExistsActivePlateAsync_ShouldReturnTrue_WhenPlateActive()
    {
        // Arrange
        var db = CreateDbContext("Test_ExistsActive_True");
        var repo = new VehicleRepository(db);
        var entry = new VehicleEntry("Carro", "ABC123", DateTime.UtcNow);
        await repo.AddAsync(entry);

        // Act
        var exists = await repo.ExistsActivePlateAsync("ABC123");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsActivePlateAsync_ShouldReturnFalse_WhenPlateHasExited()
    {
        // Arrange
        var db = CreateDbContext("Test_ExistsActive_Exited");
        var repo = new VehicleRepository(db);
        var entry = new VehicleEntry("Carro", "ABC123", DateTime.UtcNow.AddHours(-2));
        await repo.AddAsync(entry);
        entry.Exit(DateTime.UtcNow);
        await repo.UpdateAsync(entry);

        // Act
        var exists = await repo.ExistsActivePlateAsync("ABC123");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsActivePlateAsync_ShouldReturnFalse_WhenPlateNotFound()
    {
        // Arrange
        var db = CreateDbContext("Test_ExistsActive_NotFound");
        var repo = new VehicleRepository(db);

        // Act
        var exists = await repo.ExistsActivePlateAsync("NONEXIST");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var db = CreateDbContext("Test_Update_Persist");
        var repo = new VehicleRepository(db);
        var entry = new VehicleEntry("Carro", "ABC123", DateTime.UtcNow.AddHours(-3));
        await repo.AddAsync(entry);

        // Act
        var result = entry.Exit(DateTime.UtcNow);
        await repo.UpdateAsync(entry);

        // Assert
        var updated = await db.VehicleEntries.FindAsync(entry.Id);
        updated.Should().NotBeNull();
        updated!.ExitTime.Should().NotBeNull();
        updated.TotalMinutes.Should().Be(result.TotalMinutes);
        updated.Fee.Should().Be(result.Fee);
    }
}
