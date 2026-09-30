using Appointments.Application.Reservas;
using Microsoft.AspNetCore.Mvc;

namespace Appointments.Api.Controllers;

[ApiController]
[Route("api/publico/negocios")]
public class PublicoController : ControllerBase
{
    private readonly IReservasService _reservas;

    public PublicoController(IReservasService reservas) => _reservas = reservas;

    [HttpGet("{slug}")]
    public async Task<IActionResult> Negocio(string slug, CancellationToken cancellationToken)
    {
        var negocio = await _reservas.ObtenerNegocioAsync(slug, cancellationToken);
        return negocio is null ? NotFound() : Ok(negocio);
    }

    [HttpGet("{slug}/servicios")]
    public Task<IReadOnlyList<Appointments.Application.Catalogo.ServicioDto>> Servicios(string slug, CancellationToken cancellationToken) =>
        _reservas.ServiciosAsync(slug, cancellationToken);

    [HttpGet("{slug}/servicios/{servicioId:guid}/empleados")]
    public Task<IReadOnlyList<EmpleadoPublicoDto>> Empleados(string slug, Guid servicioId, CancellationToken cancellationToken) =>
        _reservas.EmpleadosAsync(slug, servicioId, cancellationToken);

    [HttpGet("{slug}/huecos")]
    public Task<HuecosDto> Huecos(string slug, Guid servicioId, Guid empleadoId, DateOnly fecha, CancellationToken cancellationToken) =>
        _reservas.HuecosAsync(slug, servicioId, empleadoId, fecha, cancellationToken);
}
