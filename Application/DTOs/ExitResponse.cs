namespace Application.DTOs;

public record ExitResponse(
    Guid Id,
    string Plate,
    string VehicleType,
    DateTime EntryTime,
    DateTime ExitTime,
    int TotalMinutes,
    decimal Fee,
    bool EmailSent
);
