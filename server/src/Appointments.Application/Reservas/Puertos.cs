using Appointments.Application.Catalogo;
using Appointments.Domain;

namespace Appointments.Application.Reservas;

/// <summary>
/// Lo que los casos de uso de reservas necesitan saber y guardar de la agenda.
/// Los implementa Infrastructure; Application no conoce cómo se persiste.
/// </summary>
public interface IAgenda
{
    Task<Negocio?> NegocioActivoPorSlugAsync(string slug, CancellationToken cancellationToken);
    Task<Negocio?> NegocioAsync(Guid negocioId, CancellationToken cancellationToken);
    Task<Servicio?> ServicioActivoAsync(Guid negocioId, Guid servicioId, CancellationToken cancellationToken);
    Task<bool> EmpleadoOfreceAsync(Guid empleadoId, Guid servicioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServicioDto>> ServiciosActivosAsync(Guid negocioId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmpleadoPublicoDto>> EmpleadosQueOfrecenAsync(Guid negocioId, Guid servicioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DisponibilidadSemanal>> FranjasAsync(Guid empleadoId, DayOfWeek dia, CancellationToken cancellationToken);

    /// <summary>Turnos confirmados y bloqueos del empleado que tocan el rango.</summary>
    Task<IReadOnlyList<Intervalo>> OcupadosAsync(
        Guid empleadoId, DateTime desdeUtc, DateTime hastaUtc, Guid? excluirTurnoId, CancellationToken cancellationToken);

    Task<bool> ClienteTieneTurnoConfirmadoAsync(
        Guid clienteId, Guid negocioId, DateTime desdeUtc, DateTime hastaUtc, Guid? excluirTurnoId, CancellationToken cancellationToken);

    Task<Turno?> TurnoAsync(Guid turnoId, CancellationToken cancellationToken);
    void Agregar(Turno turno);
    Task GuardarAsync(CancellationToken cancellationToken);

    Task<TurnoListaDto> TurnoListaAsync(Guid turnoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TurnoListaDto>> TurnosDelClienteAsync(Guid clienteId, int maximo, CancellationToken cancellationToken);
}

/// <summary>
/// Ejecuta un caso de uso de forma que dos reservas simultáneas no puedan pisarse.
/// </summary>
public interface IUnidadDeTrabajo
{
    /// <summary>Corre la acción en una transacción aislada y la reintenta si hay un interbloqueo.</summary>
    Task<T> EjecutarAsync<T>(Func<CancellationToken, Task<T>> accion, CancellationToken cancellationToken);

    /// <summary>Serializa las reservas del mismo cliente y del mismo empleado. Solo dentro de <see cref="EjecutarAsync{T}"/>.</summary>
    Task BloquearReservaAsync(Guid clienteId, Guid empleadoId, CancellationToken cancellationToken);
}
