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

    [HttpPost("entry")]
    [ProducesResponseType(typeof(EntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterEntry([FromBody] EntryRequest request, CancellationToken ct)
    {
        var response = await _parkingService.RegisterEntryAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("{id:guid}/exit")]
    [ProducesResponseType(typeof(ExitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterExit(Guid id, CancellationToken ct)
    {
        var response = await _parkingService.RegisterExitAsync(id, ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var response = await _parkingService.GetByIdAsync(id, ct);
        return Ok(response);
    }
}
