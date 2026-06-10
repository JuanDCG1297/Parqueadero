using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class EntryRequestValidator : AbstractValidator<EntryRequest>
{
    public EntryRequestValidator()
    {
        RuleFor(x => x.Plate)
            .NotEmpty().WithMessage("La placa es requerida.")
            .MaximumLength(10).WithMessage("La placa debe tener como máximo 10 caracteres.")
            .Must(p => p.All(char.IsLetterOrDigit)).WithMessage("La placa debe ser alfanumérica.")
            .Must(p => p == p.ToUpperInvariant()).WithMessage("La placa debe estar en mayúsculas.");

        RuleFor(x => x.VehicleType)
            .Must(t => t is "Carro" or "Moto")
            .WithMessage("El tipo de vehículo debe ser 'Carro' o 'Moto'.");

        RuleFor(x => x.EntryTime)
            .Must(t => t is null || t.Value <= DateTime.UtcNow)
            .WithMessage("La hora de entrada no puede ser en el futuro.");
    }
}
