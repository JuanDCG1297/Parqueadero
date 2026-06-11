using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class VehicleTypeRepository : IVehicleTypeRepository
{
    private readonly AppDbContext _db;

    public VehicleTypeRepository(AppDbContext db) => _db = db;

    public async Task<VehicleType?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.VehicleTypes.FirstOrDefaultAsync(vt => vt.Name == name, ct);

    public async Task<IReadOnlyList<VehicleType>> GetAllAsync(CancellationToken ct = default)
        => await _db.VehicleTypes.ToListAsync(ct);
}
