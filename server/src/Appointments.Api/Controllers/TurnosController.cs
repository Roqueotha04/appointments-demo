using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Appointments.Application.Catalogo;
using Appointments.Application.Reservas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Appointments.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/turnos")]
public class TurnosController : ControllerBase
{
    private readonly IReservasService _reservas;

    public TurnosController(IReservasService reservas) => _reservas = reservas;

    [HttpGet("mios")]
    public Task<IReadOnlyList<TurnoListaDto>> Mios(CancellationToken cancellationToken) =>
        _reservas.MiosAsync(UsuarioId(), cancellationToken);

    [HttpPost]
    [EnableRateLimiting("reservas")]
    public Task<TurnoListaDto> Crear(CrearTurnoRequest request, CancellationToken cancellationToken) =>
        _reservas.CrearAsync(UsuarioId(), request, cancellationToken);

    [HttpPost("{id:guid}/reprogramar")]
    [EnableRateLimiting("reservas")]
    public Task<TurnoListaDto> Reprogramar(Guid id, ReprogramarTurnoRequest request, CancellationToken cancellationToken) =>
        _reservas.ReprogramarAsync(UsuarioId(), id, request, cancellationToken);

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        await _reservas.CancelarAsync(UsuarioId(), id, cancellationToken);
        return NoContent();
    }

    private Guid UsuarioId() => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
