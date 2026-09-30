using Appointments.Domain;

namespace Appointments.Application.Catalogo;

public record NegocioDto(Guid Id, string Nombre, string Slug, string ZonaHoraria, bool Activo);
public record CrearNegocioRequest(string Nombre, string? ZonaHoraria);
public record ActualizarNegocioRequest(string Nombre, string ZonaHoraria, bool Activo);

public record ServicioDto(Guid Id, string Nombre, string Descripcion, int DuracionMinutos, decimal Precio, bool Activo);
public record GuardarServicioRequest(string Nombre, string Descripcion, int DuracionMinutos, decimal Precio);

public record EmpleadoDto(Guid Id, string Nombre, bool Activo, IReadOnlyList<Guid> ServicioIds);
public record GuardarEmpleadoRequest(string Nombre);
public record AsignarServiciosRequest(IReadOnlyList<Guid> ServicioIds);

public record FranjaDto(int DiaSemana, string HoraInicio, string HoraFin);
public record GuardarDisponibilidadRequest(IReadOnlyList<FranjaDto> Franjas);
public record BloqueoDto(Guid Id, DateTime InicioUtc, DateTime FinUtc, string? Motivo);
public record CrearBloqueoRequest(string Fecha, string HoraInicio, string HoraFin, string? Motivo);

public record TurnoListaDto(
    Guid Id,
    Guid NegocioId,
    Guid ServicioId,
    Guid EmpleadoId,
    string NegocioSlug,
    string Negocio,
    string Servicio,
    string Empleado,
    string Cliente,
    string ClienteEmail,
    DateTime InicioUtc,
    DateTime FinUtc,
    EstadoTurno Estado);

public interface ICatalogoService
{
    Task<IReadOnlyList<NegocioDto>> ListarNegociosAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<NegocioDto> CrearNegocioAsync(Guid ownerId, CrearNegocioRequest request, CancellationToken cancellationToken);
    Task<NegocioDto> ActualizarNegocioAsync(Guid ownerId, Guid negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServicioDto>> ListarServiciosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken);
    Task<ServicioDto> CrearServicioAsync(Guid ownerId, Guid negocioId, GuardarServicioRequest request, CancellationToken cancellationToken);
    Task<ServicioDto> ActualizarServicioAsync(Guid ownerId, Guid negocioId, Guid servicioId, GuardarServicioRequest request, CancellationToken cancellationToken);
    Task DesactivarServicioAsync(Guid ownerId, Guid negocioId, Guid servicioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmpleadoDto>> ListarEmpleadosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken);
    Task<EmpleadoDto> CrearEmpleadoAsync(Guid ownerId, Guid negocioId, GuardarEmpleadoRequest request, CancellationToken cancellationToken);
    Task<EmpleadoDto> ActualizarEmpleadoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, GuardarEmpleadoRequest request, CancellationToken cancellationToken);
    Task DesactivarEmpleadoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken);
    Task AsignarServiciosAsync(Guid ownerId, Guid negocioId, Guid empleadoId, AsignarServiciosRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<FranjaDto>> ObtenerDisponibilidadAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken);
    Task GuardarDisponibilidadAsync(Guid ownerId, Guid negocioId, Guid empleadoId, GuardarDisponibilidadRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BloqueoDto>> ListarBloqueosAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken);
    Task<BloqueoDto> CrearBloqueoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CrearBloqueoRequest request, CancellationToken cancellationToken);
    Task EliminarBloqueoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, Guid bloqueoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TurnoListaDto>> ListarTurnosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken);
}
