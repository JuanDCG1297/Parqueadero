using Domain.Entities;
using Domain.Exceptions;
using Application.DTOs;
using Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ParkingService : IParkingService
{
    private readonly IVehicleRepository _repo;
    private readonly IEmailService _emailService;
    private readonly IValidator<EntryRequest> _entryValidator;
    private readonly ILogger<ParkingService> _logger;

    public ParkingService(
        IVehicleRepository repo,
        IEmailService emailService,
        IValidator<EntryRequest> entryValidator,
        ILogger<ParkingService> logger)
    {
        _repo = repo;
        _emailService = emailService;
        _entryValidator = entryValidator;
        _logger = logger;
    }

    public async Task<EntryResponse> RegisterEntryAsync(EntryRequest request, CancellationToken ct = default)
    {
        // Valida datos de entrada
        var validationResult = await _entryValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entryTime = request.EntryTime ?? DateTime.UtcNow;

        // Valida placas duplicadas sin fecha de salida
        var exists = await _repo.ExistsActivePlateAsync(request.Plate.ToUpperInvariant(), ct);
        if (exists)
            throw new ConflictException($"Vehiculo con placa {request.Plate} ya está estacionado.");

        var entry = new VehicleEntry(request.VehicleType, request.Plate, entryTime);
        await _repo.AddAsync(entry, ct);

        return new EntryResponse(entry.Id, entry.VehicleType, entry.Plate, entry.EntryTime);
    }

    public async Task<ExitResponse> RegisterExitAsync(Guid id, CancellationToken ct = default)
    {
        // Valida que el vehiculo exista por id
        var entry = await _repo.GetByIdAsync(id, ct);
        if (entry is null)
            throw new NotFoundException($"Vehiculo con ID {id} no encontrado.");

        //Valida que no haya sido registrado su salida previamente y calcula el valor a pagar con un redondeo en minutos 
        var result = entry.Exit(DateTime.UtcNow);
        await _repo.UpdateAsync(entry, ct);

        //Llamado Api de envio de correo de notificacion al cliente
        await _emailService.SendExitNotificationAsync(entry, ct);

        return new ExitResponse(
            entry.Id, entry.Plate, entry.VehicleType,
            entry.EntryTime, entry.ExitTime!.Value,
            result.TotalMinutes, result.Fee, entry.EmailSent
        );
    }

    public async Task<VehicleResponse> GetByPlateAsync(string plate, CancellationToken ct = default)
    {
        // Valida que el vehiculo exista por placa
        var entry = await _repo.GetByPlateAsync(plate, ct);
        if (entry is null)
            throw new NotFoundException($"Vehiculo con placa {plate} no encontrado.");

        return new VehicleResponse(
            entry.Id, entry.Plate, entry.VehicleType,
            entry.EntryTime, entry.ExitTime,
            entry.TotalMinutes, entry.Fee, entry.EmailSent
        );
    }
}
