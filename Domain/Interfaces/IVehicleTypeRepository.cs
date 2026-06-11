using Domain.Entities;

namespace Application.Interfaces;

public interface IVehicleTypeRepository
{
    Task<VehicleType?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<VehicleType>> GetAllAsync(CancellationToken ct = default);
}
