using Appointments.Application.Catalogo;
using Appointments.Domain;

namespace Appointments.Application.Reservas;

public record NegocioPublicoDto(Guid Id, string Nombre, string Slug, string ZonaHoraria);
public record HuecosDto(IReadOnlyList<DateTime> IniciosUtc);
public record CrearTurnoRequest(Guid NegocioId, Guid ServicioId, Guid EmpleadoId, DateTimeOffset InicioUtc);
public record ReprogramarTurnoRequest(DateTimeOffset InicioUtc);

public interface IReservasService
{
    Task<NegocioPublicoDto?> ObtenerNegocioAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServicioDto>> ServiciosAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmpleadoPublicoDto>> EmpleadosAsync(string slug, Guid servicioId, CancellationToken cancellationToken);
    Task<HuecosDto> HuecosAsync(string slug, Guid servicioId, Guid empleadoId, DateOnly fecha, CancellationToken cancellationToken);
    Task<TurnoListaDto> CrearAsync(Guid clienteId, CrearTurnoRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TurnoListaDto>> MiosAsync(Guid clienteId, CancellationToken cancellationToken);
    Task<TurnoListaDto> ReprogramarAsync(Guid clienteId, Guid turnoId, ReprogramarTurnoRequest request, CancellationToken cancellationToken);
    Task CancelarAsync(Guid usuarioId, Guid turnoId, CancellationToken cancellationToken);
}

public record EmpleadoPublicoDto(Guid Id, string Nombre);
