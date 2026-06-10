using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IParkingService _parkingService;

    public VehiclesController(IParkingService parkingService)
        => _parkingService = parkingService;

    /// <summary>
    /// Metodo que registra los vehiculos que ingresan al parqueadero. Valida que no existan placas duplicadas cuando aun tienen fecha de salida null y que los datos de entrada sean correctos.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("entry")]
    [ProducesResponseType(typeof(EntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterEntry([FromBody] EntryRequest request, CancellationToken ct)
    {
        var response = await _parkingService.RegisterEntryAsync(request, ct);
        return CreatedAtAction(nameof(GetByPlate), new { plate = response.Plate }, response);
    }

    /// <summary>
    /// Metodo que registra la salida de los vehiculos del parqueadero. Valida que el vehiculo exista y que no haya sido registrado su salida previamente. Calcula el tiempo total y el valor a pagar, y envia un correo de notificacion al cliente.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/exit")]
    [ProducesResponseType(typeof(ExitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterExit(Guid id, CancellationToken ct)
    {
        var response = await _parkingService.RegisterExitAsync(id, ct);
        return Ok(response);
    }


    /// <summary>
    /// Metodo que consulta la informacion de un vehiculo registrado en el parqueadero por su placa. Valida que el vehiculo exista y retorna su informacion incluyendo si se encuentra actualmente en el parqueadero o ya ha salido.
    /// </summary>
    /// <param name="plate"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet("GetByPlate")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPlate(string plate, CancellationToken ct)
    {
        var response = await _parkingService.GetByPlateAsync(plate, ct);
        return Ok(response);
    }
}
