namespace Application.DTOs;

public record EntryRequest(
    string VehicleType,
    string Plate,
    DateTime? EntryTime
);
