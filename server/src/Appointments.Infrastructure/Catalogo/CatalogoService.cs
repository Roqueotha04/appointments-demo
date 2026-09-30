using Appointments.Application;
using Appointments.Application.Catalogo;
using Appointments.Domain;
using Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Appointments.Infrastructure.Catalogo;

public class CatalogoService : ICatalogoService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public CatalogoService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<NegocioDto>> ListarNegociosAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _db.Negocios.AsNoTracking()
            .Where(n => n.OwnerId == ownerId)
            .OrderBy(n => n.Nombre)
            .Select(n => new NegocioDto(n.Id, n.Nombre, n.Slug, n.ZonaHoraria, n.Activo))
            .ToListAsync(cancellationToken);

    public async Task<NegocioDto> CrearNegocioAsync(Guid ownerId, CrearNegocioRequest request, CancellationToken cancellationToken)
    {
        var nombre = Nombre(request.Nombre);
        var zona = string.IsNullOrWhiteSpace(request.ZonaHoraria)
            ? "America/Argentina/Buenos_Aires"
            : request.ZonaHoraria.Trim();
        ExigirZona(zona);

        var negocio = new Negocio
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Nombre = nombre,
            Slug = await SlugLibre(Slugs.Desde(nombre), cancellationToken),
            ZonaHoraria = zona,
            CreadoUtc = _clock.GetUtcNow().UtcDateTime
        };
        var empleado = new Empleado
        {
            Id = Guid.NewGuid(),
            NegocioId = negocio.Id,
            Nombre = nombre
        };
        _db.Negocios.Add(negocio);
        _db.Empleados.Add(empleado);
        _db.Disponibilidad.AddRange(HorarioInicial(empleado.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return new NegocioDto(negocio.Id, negocio.Nombre, negocio.Slug, negocio.ZonaHoraria, negocio.Activo);
    }

    public async Task<NegocioDto> ActualizarNegocioAsync(Guid ownerId, Guid negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken)
    {
        var negocio = await DelOwner(ownerId, negocioId, cancellationToken);
        negocio.Nombre = Nombre(request.Nombre);
        negocio.ZonaHoraria = request.ZonaHoraria.Trim();
        ExigirZona(negocio.ZonaHoraria);
        negocio.Activo = request.Activo;
        await _db.SaveChangesAsync(cancellationToken);
        return new NegocioDto(negocio.Id, negocio.Nombre, negocio.Slug, negocio.ZonaHoraria, negocio.Activo);
    }

    public async Task<IReadOnlyList<ServicioDto>> ListarServiciosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        return await ServiciosDe(negocioId).ToListAsync(cancellationToken);
    }

    public async Task<ServicioDto> CrearServicioAsync(Guid ownerId, Guid negocioId, GuardarServicioRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        ValidarServicio(request);
        var servicio = new Servicio
        {
            Id = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion.Trim(),
            DuracionMinutos = request.DuracionMinutos,
            Precio = request.Precio
        };
        _db.Servicios.Add(servicio);
        var empleados = await _db.Empleados.Where(e => e.NegocioId == negocioId && e.Activo).Select(e => e.Id).ToListAsync(cancellationToken);
        foreach (var empleadoId in empleados)
            _db.EmpleadoServicios.Add(new EmpleadoServicio { EmpleadoId = empleadoId, ServicioId = servicio.Id });

        await _db.SaveChangesAsync(cancellationToken);
        return ADto(servicio);
    }

    public async Task<ServicioDto> ActualizarServicioAsync(Guid ownerId, Guid negocioId, Guid servicioId, GuardarServicioRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        ValidarServicio(request);
        var servicio = await ServicioDe(negocioId, servicioId, cancellationToken);
        servicio.Nombre = request.Nombre.Trim();
        servicio.Descripcion = request.Descripcion.Trim();
        servicio.DuracionMinutos = request.DuracionMinutos;
        servicio.Precio = request.Precio;
        await _db.SaveChangesAsync(cancellationToken);
        return ADto(servicio);
    }

    public async Task DesactivarServicioAsync(Guid ownerId, Guid negocioId, Guid servicioId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var servicio = await ServicioDe(negocioId, servicioId, cancellationToken);
        servicio.Activo = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmpleadoDto>> ListarEmpleadosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var empleados = await _db.Empleados.AsNoTracking()
            .Where(e => e.NegocioId == negocioId)
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);
        var vinculos = await _db.EmpleadoServicios.AsNoTracking()
            .Where(v => empleados.Select(e => e.Id).Contains(v.EmpleadoId))
            .ToListAsync(cancellationToken);
        return empleados.Select(e => new EmpleadoDto(
            e.Id,
            e.Nombre,
            e.Activo,
            vinculos.Where(v => v.EmpleadoId == e.Id).Select(v => v.ServicioId).ToList())).ToList();
    }

    public async Task<EmpleadoDto> CrearEmpleadoAsync(Guid ownerId, Guid negocioId, GuardarEmpleadoRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var empleado = new Empleado { Id = Guid.NewGuid(), NegocioId = negocioId, Nombre = Nombre(request.Nombre) };
        _db.Empleados.Add(empleado);
        _db.Disponibilidad.AddRange(HorarioInicial(empleado.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return new EmpleadoDto(empleado.Id, empleado.Nombre, true, []);
    }

    public async Task<EmpleadoDto> ActualizarEmpleadoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, GuardarEmpleadoRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var empleado = await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        empleado.Nombre = Nombre(request.Nombre);
        await _db.SaveChangesAsync(cancellationToken);
        var servicios = await _db.EmpleadoServicios.Where(v => v.EmpleadoId == empleadoId).Select(v => v.ServicioId).ToListAsync(cancellationToken);
        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Activo, servicios);
    }

    public async Task DesactivarEmpleadoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var empleado = await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        empleado.Activo = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AsignarServiciosAsync(Guid ownerId, Guid negocioId, Guid empleadoId, AsignarServiciosRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        var ids = request.ServicioIds.Distinct().ToArray();
        var validos = await _db.Servicios.CountAsync(s => s.NegocioId == negocioId && ids.Contains(s.Id), cancellationToken);
        if (validos != ids.Length)
            throw new ReglaDeNegocioException(400, "Servicios", "Hay servicios que no pertenecen al negocio.");

        var actuales = await _db.EmpleadoServicios.Where(v => v.EmpleadoId == empleadoId).ToListAsync(cancellationToken);
        _db.EmpleadoServicios.RemoveRange(actuales);
        foreach (var servicioId in ids)
            _db.EmpleadoServicios.Add(new EmpleadoServicio { EmpleadoId = empleadoId, ServicioId = servicioId });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FranjaDto>> ObtenerDisponibilidadAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        return await _db.Disponibilidad.AsNoTracking()
            .Where(d => d.EmpleadoId == empleadoId)
            .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio)
            .Select(d => new FranjaDto((int)d.DiaSemana, d.HoraInicio.ToString("HH:mm"), d.HoraFin.ToString("HH:mm")))
            .ToListAsync(cancellationToken);
    }

    public async Task GuardarDisponibilidadAsync(Guid ownerId, Guid negocioId, Guid empleadoId, GuardarDisponibilidadRequest request, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        var franjas = request.Franjas.Select(ParsearFranja).ToList();
        foreach (var grupo in franjas.GroupBy(f => f.DiaSemana))
        {
            var ordenadas = grupo.OrderBy(f => f.HoraInicio).ToList();
            for (var i = 1; i < ordenadas.Count; i++)
            {
                if (ordenadas[i].HoraInicio < ordenadas[i - 1].HoraFin)
                    throw new ReglaDeNegocioException(400, "Horario", "Hay franjas que se pisan el mismo día.");
            }
        }

        var actuales = await _db.Disponibilidad.Where(d => d.EmpleadoId == empleadoId).ToListAsync(cancellationToken);
        _db.Disponibilidad.RemoveRange(actuales);
        _db.Disponibilidad.AddRange(franjas.Select(f => new DisponibilidadSemanal
        {
            Id = Guid.NewGuid(),
            EmpleadoId = empleadoId,
            DiaSemana = f.DiaSemana,
            HoraInicio = f.HoraInicio,
            HoraFin = f.HoraFin
        }));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BloqueoDto>> ListarBloqueosAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        return await _db.Bloqueos.AsNoTracking()
            .Where(b => b.EmpleadoId == empleadoId && b.FinUtc >= _clock.GetUtcNow().UtcDateTime)
            .OrderBy(b => b.InicioUtc)
            .Select(b => new BloqueoDto(b.Id, b.InicioUtc, b.FinUtc, b.Motivo))
            .ToListAsync(cancellationToken);
    }

    public async Task<BloqueoDto> CrearBloqueoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, CrearBloqueoRequest request, CancellationToken cancellationToken)
    {
        var negocio = await DelOwner(ownerId, negocioId, cancellationToken);
        await EmpleadoDe(negocioId, empleadoId, cancellationToken);
        var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
        var inicio = LocalAUtc(request.Fecha, request.HoraInicio, zona);
        var fin = LocalAUtc(request.Fecha, request.HoraFin, zona);
        if (fin <= inicio)
            throw new ReglaDeNegocioException(400, "Bloqueo", "La hora de fin tiene que ser posterior.");

        var bloqueo = new BloqueoAgenda
        {
            Id = Guid.NewGuid(),
            EmpleadoId = empleadoId,
            InicioUtc = inicio,
            FinUtc = fin,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim()
        };
        _db.Bloqueos.Add(bloqueo);
        await _db.SaveChangesAsync(cancellationToken);
        return new BloqueoDto(bloqueo.Id, bloqueo.InicioUtc, bloqueo.FinUtc, bloqueo.Motivo);
    }

    public async Task EliminarBloqueoAsync(Guid ownerId, Guid negocioId, Guid empleadoId, Guid bloqueoId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        var bloqueo = await _db.Bloqueos.FirstOrDefaultAsync(b => b.Id == bloqueoId && b.EmpleadoId == empleadoId, cancellationToken)
            ?? throw new ReglaDeNegocioException(404, "Bloqueo", "No encontramos ese bloqueo.");
        _db.Bloqueos.Remove(bloqueo);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TurnoListaDto>> ListarTurnosAsync(Guid ownerId, Guid negocioId, CancellationToken cancellationToken)
    {
        await DelOwner(ownerId, negocioId, cancellationToken);
        return await _db.Turnos.AsNoTracking()
            .Where(t => t.NegocioId == negocioId)
            .OrderByDescending(t => t.InicioUtc)
            .Take(200)
            .ALista(_db.Users.AsNoTracking())
            .ToListAsync(cancellationToken);
    }

    private async Task<Negocio> DelOwner(Guid ownerId, Guid negocioId, CancellationToken cancellationToken)
    {
        var negocio = await _db.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId, cancellationToken);
        if (negocio is null || negocio.OwnerId != ownerId)
            throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");
        return negocio;
    }

    private IQueryable<ServicioDto> ServiciosDe(Guid negocioId) =>
        _db.Servicios.AsNoTracking()
            .Where(s => s.NegocioId == negocioId)
            .OrderBy(s => s.Nombre)
            .Select(s => new ServicioDto(s.Id, s.Nombre, s.Descripcion, s.DuracionMinutos, s.Precio, s.Activo));

    private async Task<Servicio> ServicioDe(Guid negocioId, Guid servicioId, CancellationToken cancellationToken) =>
        await _db.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId && s.NegocioId == negocioId, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Servicio", "No encontramos ese servicio.");

    private async Task<Empleado> EmpleadoDe(Guid negocioId, Guid empleadoId, CancellationToken cancellationToken) =>
        await _db.Empleados.FirstOrDefaultAsync(e => e.Id == empleadoId && e.NegocioId == negocioId, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Empleado", "No encontramos ese empleado.");

    private async Task<string> SlugLibre(string baseSlug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "negocio";
        var slug = baseSlug;
        var i = 2;
        while (await _db.Negocios.AnyAsync(n => n.Slug == slug, cancellationToken))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private static IEnumerable<DisponibilidadSemanal> HorarioInicial(Guid empleadoId)
    {
        for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Saturday; dia++)
        {
            yield return new DisponibilidadSemanal
            {
                Id = Guid.NewGuid(),
                EmpleadoId = empleadoId,
                DiaSemana = dia,
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(18, 0)
            };
        }
    }

    private static (DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin) ParsearFranja(FranjaDto franja)
    {
        if (franja.DiaSemana is < 0 or > 6)
            throw new ReglaDeNegocioException(400, "Horario", "El día de la semana no es válido.");
        if (!TimeOnly.TryParse(franja.HoraInicio, out var inicio) || !TimeOnly.TryParse(franja.HoraFin, out var fin) || fin <= inicio)
            throw new ReglaDeNegocioException(400, "Horario", "Revisá el horario de la franja.");
        return ((DayOfWeek)franja.DiaSemana, inicio, fin);
    }

    private static DateTime LocalAUtc(string fecha, string hora, TimeZoneInfo zona)
    {
        if (!DateOnly.TryParse(fecha, out var dia) || !TimeOnly.TryParse(hora, out var reloj))
            throw new ReglaDeNegocioException(400, "Bloqueo", "La fecha o la hora no son válidas.");
        return ZonasHorarias.AUtc(dia, reloj, zona);
    }

    private static void ValidarServicio(GuardarServicioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ReglaDeNegocioException(400, "Servicio", "El nombre es obligatorio.");
        if (request.DuracionMinutos is < 10 or > 480)
            throw new ReglaDeNegocioException(400, "Servicio", "La duración tiene que estar entre 10 y 480 minutos.");
        if (request.Precio < 0)
            throw new ReglaDeNegocioException(400, "Servicio", "El precio no puede ser negativo.");
    }

    private static void ExigirZona(string zona)
    {
        try
        {
            _ = ZonasHorarias.Obtener(zona);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ReglaDeNegocioException(400, "Zona horaria", "No reconocemos esa zona horaria.");
        }
    }

    private static string Nombre(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length < 2)
            throw new ReglaDeNegocioException(400, "Datos inválidos", "El nombre es demasiado corto.");
        return valor.Trim();
    }

    private static ServicioDto ADto(Servicio servicio) =>
        new(servicio.Id, servicio.Nombre, servicio.Descripcion, servicio.DuracionMinutos, servicio.Precio, servicio.Activo);
}
