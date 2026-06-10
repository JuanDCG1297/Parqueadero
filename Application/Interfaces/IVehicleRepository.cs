using Domain.Entities;

namespace Application.Interfaces;

public interface IVehicleRepository
{
    Task<VehicleEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsActivePlateAsync(string plate, CancellationToken ct = default);
    Task AddAsync(VehicleEntry entry, CancellationToken ct = default);
    Task UpdateAsync(VehicleEntry entry, CancellationToken ct = default);
}
