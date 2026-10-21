using Appointments.Application.Catalogo;
using Appointments.Application.Reservas;
using Appointments.Domain;
using Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Appointments.Infrastructure.Reservas;

public class Agenda : IAgenda
{
    private readonly AppDbContext _db;

    public Agenda(AppDbContext db) => _db = db;

    public async Task<Negocio?> NegocioActivoPorSlugAsync(string slug, CancellationToken cancellationToken) =>
        await _db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug && n.Activo, cancellationToken);

    public async Task<Negocio?> NegocioAsync(Guid negocioId, CancellationToken cancellationToken) =>
        await _db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Id == negocioId, cancellationToken);

    public async Task<Servicio?> ServicioActivoAsync(Guid negocioId, Guid servicioId, CancellationToken cancellationToken) =>
        await _db.Servicios.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == servicioId && s.NegocioId == negocioId && s.Activo, cancellationToken);

    public async Task<bool> EmpleadoOfreceAsync(Guid empleadoId, Guid servicioId, CancellationToken cancellationToken) =>
        await _db.EmpleadoServicios.AnyAsync(
            v => v.EmpleadoId == empleadoId && v.ServicioId == servicioId && v.Empleado.Activo,
            cancellationToken);

    public async Task<IReadOnlyList<ServicioDto>> ServiciosActivosAsync(Guid negocioId, CancellationToken cancellationToken) =>
        await _db.Servicios.AsNoTracking()
            .Where(s => s.NegocioId == negocioId && s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new ServicioDto(s.Id, s.Nombre, s.Descripcion, s.DuracionMinutos, s.Precio, s.Activo))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmpleadoPublicoDto>> EmpleadosQueOfrecenAsync(Guid negocioId, Guid servicioId, CancellationToken cancellationToken) =>
        await _db.EmpleadoServicios.AsNoTracking()
            .Where(v => v.ServicioId == servicioId && v.Servicio.NegocioId == negocioId && v.Empleado.Activo && v.Servicio.Activo)
            .OrderBy(v => v.Empleado.Nombre)
            .Select(v => new EmpleadoPublicoDto(v.EmpleadoId, v.Empleado.Nombre))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DisponibilidadSemanal>> FranjasAsync(Guid empleadoId, DayOfWeek dia, CancellationToken cancellationToken) =>
        await _db.Disponibilidad.AsNoTracking()
            .Where(d => d.EmpleadoId == empleadoId && d.DiaSemana == dia)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Intervalo>> OcupadosAsync(
        Guid empleadoId, DateTime desdeUtc, DateTime hastaUtc, Guid? excluirTurnoId, CancellationToken cancellationToken)
    {
        var turnos = await _db.Turnos.AsNoTracking()
            .Where(t => t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Confirmado && t.InicioUtc < hastaUtc && t.FinUtc > desdeUtc)
            .Where(t => excluirTurnoId == null || t.Id != excluirTurnoId)
            .Select(t => new Intervalo(t.InicioUtc, t.FinUtc))
            .ToListAsync(cancellationToken);
        var bloqueos = await _db.Bloqueos.AsNoTracking()
            .Where(b => b.EmpleadoId == empleadoId && b.InicioUtc < hastaUtc && b.FinUtc > desdeUtc)
            .Select(b => new Intervalo(b.InicioUtc, b.FinUtc))
            .ToListAsync(cancellationToken);
        return [.. turnos, .. bloqueos];
    }

    public async Task<bool> ClienteTieneTurnoConfirmadoAsync(
        Guid clienteId, Guid negocioId, DateTime desdeUtc, DateTime hastaUtc, Guid? excluirTurnoId, CancellationToken cancellationToken) =>
        await _db.Turnos.AnyAsync(t =>
            t.ClienteId == clienteId
            && t.NegocioId == negocioId
            && t.Estado == EstadoTurno.Confirmado
            && t.InicioUtc >= desdeUtc
            && t.InicioUtc < hastaUtc
            && (excluirTurnoId == null || t.Id != excluirTurnoId), cancellationToken);

    public async Task<Turno?> TurnoAsync(Guid turnoId, CancellationToken cancellationToken) =>
        await _db.Turnos.FirstOrDefaultAsync(t => t.Id == turnoId, cancellationToken);

    public void Agregar(Turno turno) => _db.Turnos.Add(turno);

    public async Task GuardarAsync(CancellationToken cancellationToken) =>
        await _db.SaveChangesAsync(cancellationToken);

    public async Task<TurnoListaDto> TurnoListaAsync(Guid turnoId, CancellationToken cancellationToken) =>
        await _db.Turnos.AsNoTracking()
            .Where(t => t.Id == turnoId)
            .ALista(_db.Users.AsNoTracking())
            .FirstAsync(cancellationToken);

    public async Task<IReadOnlyList<TurnoListaDto>> TurnosDelClienteAsync(Guid clienteId, int maximo, CancellationToken cancellationToken) =>
        await _db.Turnos.AsNoTracking()
            .Where(t => t.ClienteId == clienteId)
            .OrderByDescending(t => t.InicioUtc)
            .Take(maximo)
            .ALista(_db.Users.AsNoTracking())
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TurnoListaDto>> TurnosDelNegocioAsync(
        Guid negocioId, DateTime? desdeUtc, DateTime? hastaUtc, int maximo, CancellationToken cancellationToken) =>
        await _db.Turnos.AsNoTracking()
            .Where(t => t.NegocioId == negocioId)
            .Where(t => desdeUtc == null || t.InicioUtc >= desdeUtc)
            .Where(t => hastaUtc == null || t.InicioUtc < hastaUtc)
            .OrderByDescending(t => t.InicioUtc)
            .Take(maximo)
            .ALista(_db.Users.AsNoTracking())
            .ToListAsync(cancellationToken);
}
