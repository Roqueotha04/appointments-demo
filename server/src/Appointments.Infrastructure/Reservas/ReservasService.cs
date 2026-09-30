using System.Data;
using Appointments.Application;
using Appointments.Application.Catalogo;
using Appointments.Application.Reservas;
using Appointments.Domain;
using Appointments.Infrastructure.Identity;
using Appointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Appointments.Infrastructure.Reservas;

public class ReservasService : IReservasService
{
    private readonly AppDbContext _db;
    private readonly UserManager<Usuario> _users;
    private readonly IConfirmacionCorreo _correo;
    private readonly TimeProvider _clock;

    public ReservasService(AppDbContext db, UserManager<Usuario> users, IConfirmacionCorreo correo, TimeProvider clock)
    {
        _db = db;
        _users = users;
        _correo = correo;
        _clock = clock;
    }

    public async Task<NegocioPublicoDto?> ObtenerNegocioAsync(string slug, CancellationToken cancellationToken)
    {
        var negocio = await NegocioActivo(slug, cancellationToken);
        return negocio is null ? null : new NegocioPublicoDto(negocio.Id, negocio.Nombre, negocio.Slug, negocio.ZonaHoraria);
    }

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(string slug, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        return await _db.Servicios.AsNoTracking()
            .Where(s => s.NegocioId == negocio.Id && s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new ServicioDto(s.Id, s.Nombre, s.Descripcion, s.DuracionMinutos, s.Precio, s.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmpleadoPublicoDto>> EmpleadosAsync(string slug, Guid servicioId, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        return await _db.EmpleadoServicios.AsNoTracking()
            .Where(v => v.ServicioId == servicioId && v.Servicio.NegocioId == negocio.Id && v.Empleado.Activo && v.Servicio.Activo)
            .OrderBy(v => v.Empleado.Nombre)
            .Select(v => new EmpleadoPublicoDto(v.EmpleadoId, v.Empleado.Nombre))
            .ToListAsync(cancellationToken);
    }

    public async Task<HuecosDto> HuecosAsync(string slug, Guid servicioId, Guid empleadoId, DateOnly fecha, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        var servicio = await ServicioActivo(negocio.Id, servicioId, cancellationToken);
        await EmpleadoOfrece(empleadoId, servicioId, cancellationToken);
        var huecos = await CalcularHuecos(negocio, servicio, empleadoId, fecha, excluirTurnoId: null, cancellationToken);
        return new HuecosDto(huecos);
    }

    public async Task<TurnoListaDto> CrearAsync(Guid clienteId, CrearTurnoRequest request, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var turno = await Reservar(clienteId, request.NegocioId, request.ServicioId, request.EmpleadoId, request.InicioUtc, turnoExistente: null, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await Avisar(turno, "Tu turno quedó confirmado", cancellationToken);
        return await ADto(turno.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TurnoListaDto>> MiosAsync(Guid clienteId, CancellationToken cancellationToken) =>
        await _db.Turnos.AsNoTracking()
            .Where(t => t.ClienteId == clienteId)
            .OrderByDescending(t => t.InicioUtc)
            .Take(100)
            .ALista(_db.Users.AsNoTracking())
            .ToListAsync(cancellationToken);

    public async Task<TurnoListaDto> ReprogramarAsync(Guid clienteId, Guid turnoId, ReprogramarTurnoRequest request, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var actual = await _db.Turnos.FirstOrDefaultAsync(t => t.Id == turnoId, cancellationToken)
            ?? throw new ReglaDeNegocioException(404, "Turno", "No encontramos ese turno.");
        if (actual.ClienteId != clienteId)
            throw new ReglaDeNegocioException(404, "Turno", "No encontramos ese turno.");
        if (actual.Estado != EstadoTurno.Confirmado)
            throw new ReglaDeNegocioException(409, "Turno", "Ese turno ya no se puede mover.");

        var turno = await Reservar(clienteId, actual.NegocioId, actual.ServicioId, actual.EmpleadoId, request.InicioUtc, actual, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await Avisar(turno, "Tu turno fue reprogramado", cancellationToken);
        return await ADto(turno.Id, cancellationToken);
    }

    public async Task CancelarAsync(Guid usuarioId, Guid turnoId, CancellationToken cancellationToken)
    {
        var turno = await _db.Turnos
            .Include(t => t.Negocio)
            .Include(t => t.Servicio)
            .Include(t => t.Empleado)
            .FirstOrDefaultAsync(t => t.Id == turnoId, cancellationToken)
            ?? throw new ReglaDeNegocioException(404, "Turno", "No encontramos ese turno.");

        var esCliente = turno.ClienteId == usuarioId;
        var esDueno = turno.Negocio.OwnerId == usuarioId;
        if (!esCliente && !esDueno)
            throw new ReglaDeNegocioException(404, "Turno", "No encontramos ese turno.");
        if (turno.Estado != EstadoTurno.Confirmado)
            return;

        turno.Estado = EstadoTurno.Cancelado;
        await _db.SaveChangesAsync(cancellationToken);
        await Avisar(turno, "Tu turno fue cancelado", cancellationToken);
    }

    private async Task<Turno> Reservar(
        Guid clienteId,
        Guid negocioId,
        Guid servicioId,
        Guid empleadoId,
        DateTimeOffset inicio,
        Turno? turnoExistente,
        CancellationToken cancellationToken)
    {
        var negocio = await _db.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId && n.Activo, cancellationToken)
            ?? throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");
        var servicio = await ServicioActivo(negocio.Id, servicioId, cancellationToken);
        await EmpleadoOfrece(empleadoId, servicioId, cancellationToken);

        var inicioUtc = inicio.UtcDateTime;
        var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc), zona);
        var fecha = DateOnly.FromDateTime(local);
        var huecos = await CalcularHuecos(negocio, servicio, empleadoId, fecha, turnoExistente?.Id, cancellationToken);
        if (!huecos.Any(h => Math.Abs((h - inicioUtc).TotalSeconds) < 1))
            throw new ReglaDeNegocioException(409, "Horario", "Ese horario ya no está disponible.");

        var diaInicio = fecha.ToDateTime(TimeOnly.MinValue);
        var diaFin = fecha.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var desdeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(diaInicio, DateTimeKind.Unspecified), zona);
        var hastaUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(diaFin, DateTimeKind.Unspecified), zona);

        var otroElMismoDia = await _db.Turnos.AnyAsync(t =>
            t.ClienteId == clienteId
            && t.NegocioId == negocio.Id
            && t.Estado == EstadoTurno.Confirmado
            && t.InicioUtc >= desdeUtc
            && t.InicioUtc < hastaUtc
            && (turnoExistente == null || t.Id != turnoExistente.Id), cancellationToken);

        if (otroElMismoDia)
            throw new ReglaDeNegocioException(409, "Límite diario", "Ya tenés un turno ese día en este negocio.");

        if (turnoExistente is null)
        {
            var turno = new Turno
            {
                Id = Guid.NewGuid(),
                NegocioId = negocio.Id,
                ServicioId = servicio.Id,
                EmpleadoId = empleadoId,
                ClienteId = clienteId,
                InicioUtc = inicioUtc,
                FinUtc = inicioUtc.AddMinutes(servicio.DuracionMinutos),
                Estado = EstadoTurno.Confirmado,
                CreadoUtc = _clock.GetUtcNow().UtcDateTime
            };
            _db.Turnos.Add(turno);
            await _db.SaveChangesAsync(cancellationToken);
            turno.Negocio = negocio;
            turno.Servicio = servicio;
            turno.Empleado = await _db.Empleados.FirstAsync(e => e.Id == empleadoId, cancellationToken);
            return turno;
        }

        turnoExistente.InicioUtc = inicioUtc;
        turnoExistente.FinUtc = inicioUtc.AddMinutes(servicio.DuracionMinutos);
        turnoExistente.Estado = EstadoTurno.Confirmado;
        await _db.SaveChangesAsync(cancellationToken);
        turnoExistente.Negocio = negocio;
        turnoExistente.Servicio = servicio;
        turnoExistente.Empleado = await _db.Empleados.FirstAsync(e => e.Id == empleadoId, cancellationToken);
        return turnoExistente;
    }

    private async Task<IReadOnlyList<DateTime>> CalcularHuecos(
        Negocio negocio,
        Servicio servicio,
        Guid empleadoId,
        DateOnly fecha,
        Guid? excluirTurnoId,
        CancellationToken cancellationToken)
    {
        var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
        var dia = fecha.DayOfWeek;
        var franjas = await _db.Disponibilidad.AsNoTracking()
            .Where(d => d.EmpleadoId == empleadoId && d.DiaSemana == dia)
            .ToListAsync(cancellationToken);

        var ventanas = franjas.Select(f =>
        {
            var inicio = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fecha.ToDateTime(f.HoraInicio), DateTimeKind.Unspecified), zona);
            var fin = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fecha.ToDateTime(f.HoraFin), DateTimeKind.Unspecified), zona);
            return new Intervalo(inicio, fin);
        });

        var desde = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), zona);
        var hasta = desde.AddDays(1);
        var turnos = await _db.Turnos.AsNoTracking()
            .Where(t => t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Confirmado && t.InicioUtc < hasta && t.FinUtc > desde)
            .Where(t => excluirTurnoId == null || t.Id != excluirTurnoId)
            .Select(t => new Intervalo(t.InicioUtc, t.FinUtc))
            .ToListAsync(cancellationToken);
        var bloqueos = await _db.Bloqueos.AsNoTracking()
            .Where(b => b.EmpleadoId == empleadoId && b.InicioUtc < hasta && b.FinUtc > desde)
            .Select(b => new Intervalo(b.InicioUtc, b.FinUtc))
            .ToListAsync(cancellationToken);

        var ahora = _clock.GetUtcNow().UtcDateTime;
        return CalculadoraDeHuecos.Calcular(ventanas, turnos.Concat(bloqueos), servicio.DuracionMinutos)
            .Where(h => h > ahora)
            .ToList();
    }

    private async Task<Negocio?> NegocioActivo(string slug, CancellationToken cancellationToken) =>
        await _db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug && n.Activo, cancellationToken);

    private async Task<Negocio> ExigirNegocio(string slug, CancellationToken cancellationToken) =>
        await NegocioActivo(slug, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");

    private async Task<Servicio> ServicioActivo(Guid negocioId, Guid servicioId, CancellationToken cancellationToken) =>
        await _db.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId && s.NegocioId == negocioId && s.Activo, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Servicio", "No encontramos ese servicio.");

    private async Task EmpleadoOfrece(Guid empleadoId, Guid servicioId, CancellationToken cancellationToken)
    {
        var ofrece = await _db.EmpleadoServicios.AnyAsync(
            v => v.EmpleadoId == empleadoId && v.ServicioId == servicioId && v.Empleado.Activo,
            cancellationToken);
        if (!ofrece)
            throw new ReglaDeNegocioException(404, "Empleado", "Ese empleado no realiza el servicio.");
    }

    private async Task<TurnoListaDto> ADto(Guid turnoId, CancellationToken cancellationToken) =>
        await _db.Turnos.AsNoTracking()
            .Where(t => t.Id == turnoId)
            .ALista(_db.Users.AsNoTracking())
            .FirstAsync(cancellationToken);

    private async Task Avisar(Turno turno, string asunto, CancellationToken cancellationToken)
    {
        var cliente = await _users.FindByIdAsync(turno.ClienteId.ToString());
        if (cliente?.Email is null)
            return;

        var zona = ZonasHorarias.Obtener(turno.Negocio.ZonaHoraria);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(turno.InicioUtc, DateTimeKind.Utc), zona);
        var cuerpo = $"{asunto}.\n{turno.Negocio.Nombre}\n{turno.Servicio.Nombre} con {turno.Empleado.Nombre}\n{local:dddd d 'de' MMMM, HH:mm}";
        await _correo.EnviarAsync(cliente.Email, asunto, cuerpo, cancellationToken);
    }
}
