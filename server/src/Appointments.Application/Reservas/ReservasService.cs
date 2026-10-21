using Appointments.Application.Catalogo;
using Appointments.Domain;
using Microsoft.Extensions.Logging;

namespace Appointments.Application.Reservas;

public class ReservasService : IReservasService
{
    private const int DiasMaximosDeAnticipacion = 90;

    private readonly IAgenda _agenda;
    private readonly IUnidadDeTrabajo _trabajo;
    private readonly IConfirmacionCorreo _correo;
    private readonly TimeProvider _clock;
    private readonly ILogger<ReservasService> _log;

    public ReservasService(IAgenda agenda, IUnidadDeTrabajo trabajo, IConfirmacionCorreo correo, TimeProvider clock, ILogger<ReservasService> log)
    {
        _agenda = agenda;
        _trabajo = trabajo;
        _correo = correo;
        _clock = clock;
        _log = log;
    }

    public async Task<NegocioPublicoDto?> ObtenerNegocioAsync(string slug, CancellationToken cancellationToken)
    {
        var negocio = await _agenda.NegocioActivoPorSlugAsync(slug, cancellationToken);
        return negocio is null ? null : new NegocioPublicoDto(negocio.Id, negocio.Nombre, negocio.Slug, negocio.ZonaHoraria);
    }

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(string slug, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        return await _agenda.ServiciosActivosAsync(negocio.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<EmpleadoPublicoDto>> EmpleadosAsync(string slug, Guid servicioId, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        return await _agenda.EmpleadosQueOfrecenAsync(negocio.Id, servicioId, cancellationToken);
    }

    public async Task<HuecosDto> HuecosAsync(string slug, Guid servicioId, Guid empleadoId, DateOnly fecha, CancellationToken cancellationToken)
    {
        var negocio = await ExigirNegocio(slug, cancellationToken);
        var servicio = await ExigirServicio(negocio.Id, servicioId, cancellationToken);
        await ExigirQueOfrece(empleadoId, servicioId, cancellationToken);
        var huecos = await CalcularHuecos(negocio, servicio, empleadoId, fecha, excluirTurnoId: null, cancellationToken);
        return new HuecosDto(huecos);
    }

    public async Task<TurnoListaDto> CrearAsync(Guid clienteId, CrearTurnoRequest request, CancellationToken cancellationToken)
    {
        var turnoId = await _trabajo.EjecutarAsync(
            ct => Reservar(clienteId, request.NegocioId, request.ServicioId, request.EmpleadoId, request.InicioUtc, turnoId: null, ct),
            cancellationToken);
        return await Confirmar(turnoId, "Tu turno quedó confirmado", cancellationToken);
    }

    public Task<IReadOnlyList<TurnoListaDto>> MiosAsync(Guid clienteId, CancellationToken cancellationToken) =>
        _agenda.TurnosDelClienteAsync(clienteId, 100, cancellationToken);

    public async Task<IReadOnlyList<TurnoListaDto>> TurnosDelNegocioAsync(
        Guid duenoId, Guid negocioId, DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken)
    {
        var negocio = await _agenda.NegocioAsync(negocioId, cancellationToken);
        if (negocio is null || negocio.OwnerId != duenoId)
            throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");
        if (desde is not null && hasta is not null && hasta < desde)
            throw new ReglaDeNegocioException(400, "Rango", "La fecha final no puede ser anterior a la inicial.");

        var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
        DateTime? desdeUtc = desde is null ? null : ZonasHorarias.LimitesDelDiaUtc(desde.Value, zona).DesdeUtc;
        DateTime? hastaUtc = hasta is null ? null : ZonasHorarias.LimitesDelDiaUtc(hasta.Value, zona).HastaUtc;
        return await _agenda.TurnosDelNegocioAsync(negocio.Id, desdeUtc, hastaUtc, 200, cancellationToken);
    }

    public async Task<TurnoListaDto> ReprogramarAsync(Guid clienteId, Guid turnoId, ReprogramarTurnoRequest request, CancellationToken cancellationToken)
    {
        var id = await _trabajo.EjecutarAsync(async ct =>
        {
            var actual = await _agenda.TurnoAsync(turnoId, ct) ?? throw TurnoNoEncontrado();
            if (actual.ClienteId != clienteId)
                throw TurnoNoEncontrado();
            if (actual.Estado != EstadoTurno.Confirmado)
                throw new ReglaDeNegocioException(409, "Turno", "Ese turno ya no se puede mover.");
            if (actual.InicioUtc <= _clock.GetUtcNow().UtcDateTime)
                throw new ReglaDeNegocioException(409, "Turno", "Ese turno ya empezó y no se puede mover.");

            return await Reservar(clienteId, actual.NegocioId, actual.ServicioId, actual.EmpleadoId, request.InicioUtc, actual.Id, ct);
        }, cancellationToken);
        return await Confirmar(id, "Tu turno fue reprogramado", cancellationToken);
    }

    public async Task CancelarAsync(Guid usuarioId, Guid turnoId, CancellationToken cancellationToken)
    {
        var cancelado = await _trabajo.EjecutarAsync<Guid?>(async ct =>
        {
            var turno = await _agenda.TurnoAsync(turnoId, ct) ?? throw TurnoNoEncontrado();
            var negocio = await _agenda.NegocioAsync(turno.NegocioId, ct) ?? throw TurnoNoEncontrado();

            var esCliente = turno.ClienteId == usuarioId;
            var esDueno = negocio.OwnerId == usuarioId;
            if (!esCliente && !esDueno)
                throw TurnoNoEncontrado();
            if (turno.Estado != EstadoTurno.Confirmado)
                return null;
            if (turno.InicioUtc <= _clock.GetUtcNow().UtcDateTime)
                throw new ReglaDeNegocioException(409, "Turno", "Ese turno ya empezó y no se puede cancelar.");

            // Mismo candado que al reservar: no se cancela a la vez que otro lo mueve u otro lo cancela.
            await _trabajo.BloquearReservaAsync(turno.ClienteId, turno.EmpleadoId, ct);

            turno.Estado = EstadoTurno.Cancelado;
            await _agenda.GuardarAsync(ct);
            return turno.Id;
        }, cancellationToken);

        if (cancelado is { } id)
            await Confirmar(id, "Tu turno fue cancelado", cancellationToken);
    }

    /// <summary>Crea el turno, o mueve el existente si se pasa su id. Devuelve el id del turno guardado.</summary>
    private async Task<Guid> Reservar(
        Guid clienteId,
        Guid negocioId,
        Guid servicioId,
        Guid empleadoId,
        DateTimeOffset inicio,
        Guid? turnoId,
        CancellationToken cancellationToken)
    {
        var negocio = await _agenda.NegocioAsync(negocioId, cancellationToken);
        if (negocio is null || !negocio.Activo)
            throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");
        var servicio = await ExigirServicio(negocio.Id, servicioId, cancellationToken);
        await ExigirQueOfrece(empleadoId, servicioId, cancellationToken);
        await _trabajo.BloquearReservaAsync(clienteId, empleadoId, cancellationToken);

        var inicioUtc = inicio.UtcDateTime;
        var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc), zona);
        var fecha = DateOnly.FromDateTime(local);
        var huecos = await CalcularHuecos(negocio, servicio, empleadoId, fecha, turnoId, cancellationToken);
        if (!huecos.Any(h => Math.Abs((h - inicioUtc).TotalSeconds) < 1))
            throw new ReglaDeNegocioException(409, "Horario", "Ese horario ya no está disponible.");

        var (desdeUtc, hastaUtc) = ZonasHorarias.LimitesDelDiaUtc(fecha, zona);
        var otroElMismoDia = await _agenda.ClienteTieneTurnoConfirmadoAsync(
            clienteId, negocio.Id, desdeUtc, hastaUtc, turnoId, cancellationToken);
        if (otroElMismoDia)
            throw new ReglaDeNegocioException(409, "Límite diario", "Ya tenés un turno ese día en este negocio.");

        var finUtc = inicioUtc.AddMinutes(servicio.DuracionMinutos);
        if (turnoId is null)
        {
            var turno = new Turno
            {
                Id = Guid.NewGuid(),
                NegocioId = negocio.Id,
                ServicioId = servicio.Id,
                EmpleadoId = empleadoId,
                ClienteId = clienteId,
                InicioUtc = inicioUtc,
                FinUtc = finUtc,
                Estado = EstadoTurno.Confirmado,
                CreadoUtc = _clock.GetUtcNow().UtcDateTime
            };
            _agenda.Agregar(turno);
            await _agenda.GuardarAsync(cancellationToken);
            return turno.Id;
        }

        var existente = await _agenda.TurnoAsync(turnoId.Value, cancellationToken) ?? throw TurnoNoEncontrado();
        existente.InicioUtc = inicioUtc;
        existente.FinUtc = finUtc;
        existente.Estado = EstadoTurno.Confirmado;
        await _agenda.GuardarAsync(cancellationToken);
        return existente.Id;
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
        var franjas = await _agenda.FranjasAsync(empleadoId, fecha.DayOfWeek, cancellationToken);
        var ventanas = franjas.Select(f => new Intervalo(
            ZonasHorarias.AUtc(fecha, f.HoraInicio, zona),
            ZonasHorarias.AUtc(fecha, f.HoraFin, zona)));

        var (desde, hasta) = ZonasHorarias.LimitesDelDiaUtc(fecha, zona);
        var ocupados = await _agenda.OcupadosAsync(empleadoId, desde, hasta, excluirTurnoId, cancellationToken);

        var ahora = _clock.GetUtcNow().UtcDateTime;
        var limite = ahora.AddDays(DiasMaximosDeAnticipacion);
        return CalculadoraDeHuecos.Calcular(ventanas, ocupados, servicio.DuracionMinutos)
            .Where(h => h > ahora && h <= limite)
            .ToList();
    }

    private async Task<Negocio> ExigirNegocio(string slug, CancellationToken cancellationToken) =>
        await _agenda.NegocioActivoPorSlugAsync(slug, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Negocio", "No encontramos ese negocio.");

    private async Task<Servicio> ExigirServicio(Guid negocioId, Guid servicioId, CancellationToken cancellationToken) =>
        await _agenda.ServicioActivoAsync(negocioId, servicioId, cancellationToken)
        ?? throw new ReglaDeNegocioException(404, "Servicio", "No encontramos ese servicio.");

    private async Task ExigirQueOfrece(Guid empleadoId, Guid servicioId, CancellationToken cancellationToken)
    {
        if (!await _agenda.EmpleadoOfreceAsync(empleadoId, servicioId, cancellationToken))
            throw new ReglaDeNegocioException(404, "Empleado", "Ese empleado no realiza el servicio.");
    }

    private static ReglaDeNegocioException TurnoNoEncontrado() =>
        new(404, "Turno", "No encontramos ese turno.");

    /// <summary>El turno ya está guardado: devuelve su vista y avisa por mail sin que un fallo del correo tumbe la respuesta.</summary>
    private async Task<TurnoListaDto> Confirmar(Guid turnoId, string asunto, CancellationToken cancellationToken)
    {
        var dto = await _agenda.TurnoListaAsync(turnoId, cancellationToken);
        await Avisar(dto, asunto, cancellationToken);
        return dto;
    }

    private async Task Avisar(TurnoListaDto turno, string asunto, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(turno.ClienteEmail))
                return;

            var negocio = await _agenda.NegocioAsync(turno.NegocioId, cancellationToken);
            if (negocio is null)
                return;

            var zona = ZonasHorarias.Obtener(negocio.ZonaHoraria);
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(turno.InicioUtc, DateTimeKind.Utc), zona);
            var cuerpo = $"{asunto}.\n{turno.Negocio}\n{turno.Servicio} con {turno.Empleado}\n{local:dddd d 'de' MMMM, HH:mm}";
            await _correo.EnviarAsync(turno.ClienteEmail, asunto, cuerpo, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "No se pudo enviar el correo del turno {TurnoId} ({Asunto}).", turno.Id, asunto);
        }
    }
}
