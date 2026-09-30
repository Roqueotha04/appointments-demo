using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Appointments.Application.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appointments.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/negocios")]
public class NegociosController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public NegociosController(ICatalogoService catalogo) => _catalogo = catalogo;

    [HttpGet]
    public Task<IReadOnlyList<NegocioDto>> Listar(CancellationToken cancellationToken) =>
        _catalogo.ListarNegociosAsync(UsuarioId(), cancellationToken);

    [HttpPost]
    public Task<NegocioDto> Crear(CrearNegocioRequest request, CancellationToken cancellationToken) =>
        _catalogo.CrearNegocioAsync(UsuarioId(), request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<NegocioDto> Actualizar(Guid id, ActualizarNegocioRequest request, CancellationToken cancellationToken) =>
        _catalogo.ActualizarNegocioAsync(UsuarioId(), id, request, cancellationToken);

    [HttpGet("{id:guid}/servicios")]
    public Task<IReadOnlyList<ServicioDto>> Servicios(Guid id, CancellationToken cancellationToken) =>
        _catalogo.ListarServiciosAsync(UsuarioId(), id, cancellationToken);

    [HttpPost("{id:guid}/servicios")]
    public Task<ServicioDto> CrearServicio(Guid id, GuardarServicioRequest request, CancellationToken cancellationToken) =>
        _catalogo.CrearServicioAsync(UsuarioId(), id, request, cancellationToken);

    [HttpPut("{id:guid}/servicios/{servicioId:guid}")]
    public Task<ServicioDto> ActualizarServicio(Guid id, Guid servicioId, GuardarServicioRequest request, CancellationToken cancellationToken) =>
        _catalogo.ActualizarServicioAsync(UsuarioId(), id, servicioId, request, cancellationToken);

    [HttpDelete("{id:guid}/servicios/{servicioId:guid}")]
    public async Task<IActionResult> DesactivarServicio(Guid id, Guid servicioId, CancellationToken cancellationToken)
    {
        await _catalogo.DesactivarServicioAsync(UsuarioId(), id, servicioId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/empleados")]
    public Task<IReadOnlyList<EmpleadoDto>> Empleados(Guid id, CancellationToken cancellationToken) =>
        _catalogo.ListarEmpleadosAsync(UsuarioId(), id, cancellationToken);

    [HttpPost("{id:guid}/empleados")]
    public Task<EmpleadoDto> CrearEmpleado(Guid id, GuardarEmpleadoRequest request, CancellationToken cancellationToken) =>
        _catalogo.CrearEmpleadoAsync(UsuarioId(), id, request, cancellationToken);

    [HttpPut("{id:guid}/empleados/{empleadoId:guid}")]
    public Task<EmpleadoDto> ActualizarEmpleado(Guid id, Guid empleadoId, GuardarEmpleadoRequest request, CancellationToken cancellationToken) =>
        _catalogo.ActualizarEmpleadoAsync(UsuarioId(), id, empleadoId, request, cancellationToken);

    [HttpDelete("{id:guid}/empleados/{empleadoId:guid}")]
    public async Task<IActionResult> DesactivarEmpleado(Guid id, Guid empleadoId, CancellationToken cancellationToken)
    {
        await _catalogo.DesactivarEmpleadoAsync(UsuarioId(), id, empleadoId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/empleados/{empleadoId:guid}/servicios")]
    public async Task<IActionResult> AsignarServicios(Guid id, Guid empleadoId, AsignarServiciosRequest request, CancellationToken cancellationToken)
    {
        await _catalogo.AsignarServiciosAsync(UsuarioId(), id, empleadoId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/empleados/{empleadoId:guid}/disponibilidad")]
    public Task<IReadOnlyList<FranjaDto>> Disponibilidad(Guid id, Guid empleadoId, CancellationToken cancellationToken) =>
        _catalogo.ObtenerDisponibilidadAsync(UsuarioId(), id, empleadoId, cancellationToken);

    [HttpPut("{id:guid}/empleados/{empleadoId:guid}/disponibilidad")]
    public async Task<IActionResult> GuardarDisponibilidad(Guid id, Guid empleadoId, GuardarDisponibilidadRequest request, CancellationToken cancellationToken)
    {
        await _catalogo.GuardarDisponibilidadAsync(UsuarioId(), id, empleadoId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/empleados/{empleadoId:guid}/bloqueos")]
    public Task<IReadOnlyList<BloqueoDto>> Bloqueos(Guid id, Guid empleadoId, CancellationToken cancellationToken) =>
        _catalogo.ListarBloqueosAsync(UsuarioId(), id, empleadoId, cancellationToken);

    [HttpPost("{id:guid}/empleados/{empleadoId:guid}/bloqueos")]
    public Task<BloqueoDto> CrearBloqueo(Guid id, Guid empleadoId, CrearBloqueoRequest request, CancellationToken cancellationToken) =>
        _catalogo.CrearBloqueoAsync(UsuarioId(), id, empleadoId, request, cancellationToken);

    [HttpDelete("{id:guid}/empleados/{empleadoId:guid}/bloqueos/{bloqueoId:guid}")]
    public async Task<IActionResult> EliminarBloqueo(Guid id, Guid empleadoId, Guid bloqueoId, CancellationToken cancellationToken)
    {
        await _catalogo.EliminarBloqueoAsync(UsuarioId(), id, empleadoId, bloqueoId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/turnos")]
    public Task<IReadOnlyList<TurnoListaDto>> Turnos(Guid id, CancellationToken cancellationToken) =>
        _catalogo.ListarTurnosAsync(UsuarioId(), id, cancellationToken);

    private Guid UsuarioId() => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
