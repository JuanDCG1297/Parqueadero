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
    private readonly IVehicleTypeRepository _typeRepo;
    private readonly IEmailService _emailService;
    private readonly IValidator<EntryRequest> _entryValidator;
    private readonly ILogger<ParkingService> _logger;

    public ParkingService(
        IVehicleRepository repo,
        IVehicleTypeRepository typeRepo,
        IEmailService emailService,
        IValidator<EntryRequest> entryValidator,
        ILogger<ParkingService> logger)
    {
        _repo = repo;
        _typeRepo = typeRepo;
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

        // Resuelve VehicleTypeId desde el nombre (el validador ya aseguró que es "Carro" o "Moto")
        var vehicleType = await _typeRepo.GetByNameAsync(request.VehicleType, ct)
            ?? throw new NotFoundException($"Tipo de vehículo '{request.VehicleType}' no encontrado.");

        var entry = new VehicleEntry(vehicleType.Id, request.Plate, entryTime);
        await _repo.AddAsync(entry, ct);

        return new EntryResponse(entry.Id, vehicleType.Name, entry.Plate, entry.EntryTime);
    }

    public async Task<ExitResponse> RegisterExitAsync(Guid id, CancellationToken ct = default)
    {
        // Valida que el vehiculo exista por id
        var entry = await _repo.GetByIdAsync(id, ct);
        if (entry is null)
            throw new NotFoundException($"Vehiculo con ID {id} no encontrado.");

        //Valida que no haya sido registrado su salida previamente y calcula el valor a pagar con un redondeo en minutos 
        var result = entry.Exit(DateTime.UtcNow);

        // Guarda la salida PRIMERO — esto SIEMPRE debe persistir,
        // incluso si el email falla o el cliente se desconecta.
        await _repo.UpdateAsync(entry, ct);

        // Intento de envío de email — NO BLOQUEANTE.
        // Usamos CancellationToken.None para que un timeout del cliente
        // no interrumpa el envío ni afecte el registro de salida.
        try
        {
            await _emailService.SendExitNotificationAsync(entry, CancellationToken.None);

            // Si el email se envió correctamente, MarkEmailSent() cambió EmailSent
            // en memoria. Persistimos ese cambio en DB.
            if (entry.EmailSent)
            {
                await _repo.UpdateAsync(entry, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enviar notificación email para {Plate}. La salida se registró correctamente.", entry.Plate);
        }

        return new ExitResponse(
            entry.Id, entry.Plate, entry.VehicleType.Name,
            entry.EntryTime, entry.ExitTime!.Value,
            result.TotalMinutes, result.Fee, entry.EmailSent
        );
    }

    public async Task<List<VehicleResponse>> GetActiveAsync(CancellationToken ct = default)
    {
        var activeEntries = await _repo.GetActiveAsync(ct);

        return activeEntries.Select(e => new VehicleResponse(
            e.Id, e.Plate, e.VehicleType.Name,
            e.EntryTime, e.ExitTime,
            e.TotalMinutes, e.Fee, e.EmailSent
        )).ToList();
    }

    public async Task<VehicleResponse> GetByPlateAsync(string plate, CancellationToken ct = default)
    {
        // Valida que el vehiculo exista por placa
        var entry = await _repo.GetByPlateAsync(plate, ct);
        if (entry is null)
            throw new NotFoundException($"Vehiculo con placa {plate} no encontrado.");

        return new VehicleResponse(
            entry.Id, entry.Plate, entry.VehicleType.Name,
            entry.EntryTime, entry.ExitTime,
            entry.TotalMinutes, entry.Fee, entry.EmailSent
        );
    }
}
