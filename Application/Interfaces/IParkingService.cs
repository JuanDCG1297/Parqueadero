using Application.DTOs;

namespace Application.Interfaces;

public interface IParkingService
{
    Task<EntryResponse> RegisterEntryAsync(EntryRequest request, CancellationToken ct = default);
    Task<ExitResponse> RegisterExitAsync(Guid id, CancellationToken ct = default);
    Task<VehicleResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
}
