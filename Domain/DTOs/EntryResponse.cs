namespace Application.DTOs;

public record EntryResponse(
    Guid Id,
    string VehicleType,
    string Plate,
    DateTime EntryTime
);
