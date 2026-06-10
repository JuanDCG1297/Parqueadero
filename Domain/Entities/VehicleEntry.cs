using Domain.Exceptions;

namespace Domain.Entities;

public class VehicleEntry
{
    public Guid Id { get; private set; }
    public string VehicleType { get; private set; } = string.Empty;
    public string Plate { get; private set; } = string.Empty;
    public DateTime EntryTime { get; private set; }
    public DateTime? ExitTime { get; private set; }
    public int? TotalMinutes { get; private set; }
    public decimal? Fee { get; private set; }
    public bool EmailSent { get; private set; }

    private VehicleEntry() { } // EF Core

    public VehicleEntry(string vehicleType, string plate, DateTime entryTime)
    {
        Id = Guid.NewGuid();
        VehicleType = vehicleType;
        Plate = plate.ToUpperInvariant();
        EntryTime = entryTime;
        EmailSent = false;
    }

    public ExitResult Exit(DateTime exitTime)
    {
        if (ExitTime.HasValue)
            throw new ConflictException("Vehiculo ya no se encuentra en el parqueadero.");

        ExitTime = exitTime;
        TotalMinutes = (int)Math.Ceiling((exitTime - EntryTime).TotalMinutes);
        Fee = Math.Round(TotalMinutes.Value * 50m, 2);
        return new ExitResult(TotalMinutes.Value, Fee.Value);
    }

    public void MarkEmailSent() => EmailSent = true;
}

public record ExitResult(int TotalMinutes, decimal Fee);
