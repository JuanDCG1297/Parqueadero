using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _db;

    public VehicleRepository(AppDbContext db) => _db = db;

    public async Task<VehicleEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.VehicleEntries
            .Include(e => e.VehicleType)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<VehicleEntry?> GetByPlateAsync(string plate, CancellationToken ct = default)
        => await _db.VehicleEntries
            .Include(e => e.VehicleType)
            .FirstOrDefaultAsync(e => e.Plate == plate && e.ExitTime == null, ct);

    public async Task<bool> ExistsActivePlateAsync(string plate, CancellationToken ct = default)
        => await _db.VehicleEntries.AnyAsync(e => e.Plate == plate && e.ExitTime == null, ct);

    public async Task<List<VehicleEntry>> GetActiveAsync(CancellationToken ct = default)
        => await _db.VehicleEntries
            .Include(e => e.VehicleType)
            .Where(e => e.ExitTime == null)
            .ToListAsync(ct);

    public async Task AddAsync(VehicleEntry entry, CancellationToken ct = default)
    {
        await _db.VehicleEntries.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(VehicleEntry entry, CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
