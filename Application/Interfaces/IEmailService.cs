using Domain.Entities;

namespace Application.Interfaces;

public interface IEmailService
{
    Task SendExitNotificationAsync(VehicleEntry entry, CancellationToken ct = default);
}
