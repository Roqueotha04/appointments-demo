using Appointments.Application;
using Appointments.Application.Auth;
using Appointments.Application.Reservas;
using Appointments.Domain;
using Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Appointments.Infrastructure.Tests;

[Collection(ColeccionMySql.Nombre)]
public class ReservasYAuthTests
{
    private readonly BaseMySql _mysql;

    public ReservasYAuthTests(BaseMySql mysql) => _mysql = mysql;

    [Fact]
    public async Task Dos_clientes_no_pueden_quedarse_el_mismo_hueco()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        await using var servicios = BaseMySql.Crear(_mysql.ConnectionString, reloj);
        var escenario = await ArmarNegocio(servicios);
        var inicio = new DateTimeOffset(2026, 9, 28, 13, 0, 0, TimeSpan.Zero);
        var pedido = new CrearTurnoRequest(escenario.NegocioId, escenario.ServicioId, escenario.EmpleadoId, inicio);

        await using var uno = servicios.CreateAsyncScope();
        await using var dos = servicios.CreateAsyncScope();
        var primera = uno.ServiceProvider.GetRequiredService<IReservasService>()
            .CrearAsync(escenario.Cliente1, pedido, CancellationToken.None);
        var segunda = dos.ServiceProvider.GetRequiredService<IReservasService>()
            .CrearAsync(escenario.Cliente2, pedido, CancellationToken.None);

        var resultados = await Task.WhenAll(Capturar(primera), Capturar(segunda));

        Assert.Equal(1, resultados.Count(r => r is null));
        var fallo = Assert.Single(resultados, r => r is not null);
        Assert.Equal(409, fallo!.Status);

        await using var consulta = servicios.CreateAsyncScope();
        var db = consulta.ServiceProvider.GetRequiredService<AppDbContext>();
        var confirmados = await db.Turnos.CountAsync(t =>
            t.EmpleadoId == escenario.EmpleadoId && t.Estado == EstadoTurno.Confirmado);
        Assert.Equal(1, confirmados);
    }

    [Fact]
    public async Task Un_cliente_no_puede_tener_dos_turnos_el_mismo_dia_pero_si_mover_el_propio()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        await using var servicios = BaseMySql.Crear(_mysql.ConnectionString, reloj);
        var e = await ArmarNegocio(servicios);
        await using var scope = servicios.CreateAsyncScope();
        var reservas = scope.ServiceProvider.GetRequiredService<IReservasService>();
        var trece = new DateTimeOffset(2026, 9, 28, 13, 0, 0, TimeSpan.Zero);

        var turno = await reservas.CrearAsync(e.Cliente1, new CrearTurnoRequest(e.NegocioId, e.ServicioId, e.EmpleadoId, trece), CancellationToken.None);

        var segundo = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.CrearAsync(e.Cliente1, new CrearTurnoRequest(e.NegocioId, e.ServicioId, e.EmpleadoId, trece.AddMinutes(60)), CancellationToken.None));
        Assert.Equal(409, segundo.Status);
        Assert.Equal("Límite diario", segundo.Titulo);

        var movido = await reservas.ReprogramarAsync(e.Cliente1, turno.Id, new ReprogramarTurnoRequest(trece.AddMinutes(60)), CancellationToken.None);
        Assert.Equal(turno.Id, movido.Id);
        Assert.Equal(trece.AddMinutes(60).UtcDateTime, movido.InicioUtc);
        Assert.Equal(EstadoTurno.Confirmado, movido.Estado);
    }

    [Fact]
    public async Task Solo_el_cliente_mueve_su_turno_y_cancelan_el_cliente_o_el_dueno()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        await using var servicios = BaseMySql.Crear(_mysql.ConnectionString, reloj);
        var e = await ArmarNegocio(servicios);
        await using var scope = servicios.CreateAsyncScope();
        var reservas = scope.ServiceProvider.GetRequiredService<IReservasService>();
        var trece = new DateTimeOffset(2026, 9, 28, 13, 0, 0, TimeSpan.Zero);
        var turno = await reservas.CrearAsync(e.Cliente1, new CrearTurnoRequest(e.NegocioId, e.ServicioId, e.EmpleadoId, trece), CancellationToken.None);

        var ajenoMueve = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.ReprogramarAsync(e.Cliente2, turno.Id, new ReprogramarTurnoRequest(trece.AddMinutes(60)), CancellationToken.None));
        Assert.Equal(404, ajenoMueve.Status);

        var ajenoCancela = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.CancelarAsync(e.Cliente2, turno.Id, CancellationToken.None));
        Assert.Equal(404, ajenoCancela.Status);

        await reservas.CancelarAsync(e.Dueno, turno.Id, CancellationToken.None);
        var mios = await reservas.MiosAsync(e.Cliente1, CancellationToken.None);
        Assert.Equal(EstadoTurno.Cancelado, Assert.Single(mios).Estado);

        var yaCancelado = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.ReprogramarAsync(e.Cliente1, turno.Id, new ReprogramarTurnoRequest(trece.AddMinutes(60)), CancellationToken.None));
        Assert.Equal(409, yaCancelado.Status);
    }

    [Fact]
    public async Task No_se_puede_mover_ni_cancelar_un_turno_que_ya_empezo()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        await using var servicios = BaseMySql.Crear(_mysql.ConnectionString, reloj);
        var e = await ArmarNegocio(servicios);
        await using var scope = servicios.CreateAsyncScope();
        var reservas = scope.ServiceProvider.GetRequiredService<IReservasService>();
        var trece = new DateTimeOffset(2026, 9, 28, 13, 0, 0, TimeSpan.Zero);
        var turno = await reservas.CrearAsync(e.Cliente1, new CrearTurnoRequest(e.NegocioId, e.ServicioId, e.EmpleadoId, trece), CancellationToken.None);

        reloj.Avanzar(TimeSpan.FromMinutes(90));

        var mueve = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.ReprogramarAsync(e.Cliente1, turno.Id, new ReprogramarTurnoRequest(trece.AddMinutes(120)), CancellationToken.None));
        Assert.Equal(409, mueve.Status);

        var cancela = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            reservas.CancelarAsync(e.Cliente1, turno.Id, CancellationToken.None));
        Assert.Equal(409, cancela.Status);

        var mios = await reservas.MiosAsync(e.Cliente1, CancellationToken.None);
        Assert.Equal(EstadoTurno.Confirmado, Assert.Single(mios).Estado);
    }

    [Fact]
    public async Task El_quinto_intento_bloquea_la_cuenta()
    {
        await using var servicios = BaseMySql.Crear(_mysql.ConnectionString, TimeProvider.System);
        await using var scope = servicios.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var email = $"lock-{Guid.NewGuid():N}@local.test";
        await auth.RegistrarAsync(new RegistroRequest("Ana Cliente", email, "Clave-123", null), CancellationToken.None);

        for (var i = 0; i < 4; i++)
        {
            var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                auth.LoginAsync(new LoginRequest(email, "incorrecta"), CancellationToken.None));
            Assert.Equal("Credenciales", error.Titulo);
        }

        var bloqueo = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            auth.LoginAsync(new LoginRequest(email, "incorrecta"), CancellationToken.None));
        Assert.Equal("Cuenta bloqueada", bloqueo.Titulo);

        var conClaveBuena = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            auth.LoginAsync(new LoginRequest(email, "Clave-123"), CancellationToken.None));
        Assert.Equal("Cuenta bloqueada", conClaveBuena.Titulo);
    }

    private static async Task<Escenario> ArmarNegocio(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var dueno = await auth.RegistrarAsync(new RegistroRequest("Dueño Local", $"dueno-{sufijo}@local.test", "Clave-123", null), CancellationToken.None);
        var cliente1 = await auth.RegistrarAsync(new RegistroRequest("Cliente Uno", $"c1-{sufijo}@local.test", "Clave-123", null), CancellationToken.None);
        var cliente2 = await auth.RegistrarAsync(new RegistroRequest("Cliente Dos", $"c2-{sufijo}@local.test", "Clave-123", null), CancellationToken.None);

        var negocio = new Negocio
        {
            Id = Guid.NewGuid(),
            OwnerId = dueno.Sesion.Usuario.Id,
            Nombre = "Estudio",
            Slug = $"estudio-{sufijo}",
            ZonaHoraria = "America/Argentina/Buenos_Aires",
            CreadoUtc = DateTime.UtcNow
        };
        var empleado = new Empleado { Id = Guid.NewGuid(), NegocioId = negocio.Id, Nombre = "Lucía" };
        var servicio = new Servicio
        {
            Id = Guid.NewGuid(),
            NegocioId = negocio.Id,
            Nombre = "Corte",
            Descripcion = "Corte",
            DuracionMinutos = 30,
            Precio = 1000
        };
        db.Negocios.Add(negocio);
        db.Empleados.Add(empleado);
        db.Servicios.Add(servicio);
        db.EmpleadoServicios.Add(new EmpleadoServicio { EmpleadoId = empleado.Id, ServicioId = servicio.Id });
        db.Disponibilidad.Add(new DisponibilidadSemanal
        {
            Id = Guid.NewGuid(),
            EmpleadoId = empleado.Id,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 0),
            HoraFin = new TimeOnly(18, 0)
        });
        await db.SaveChangesAsync();
        return new Escenario(negocio.Id, servicio.Id, empleado.Id, cliente1.Sesion.Usuario.Id, cliente2.Sesion.Usuario.Id, dueno.Sesion.Usuario.Id);
    }

    private static async Task<ReglaDeNegocioException?> Capturar(Task tarea)
    {
        try
        {
            await tarea;
            return null;
        }
        catch (ReglaDeNegocioException ex)
        {
            return ex;
        }
    }

    private sealed record Escenario(Guid NegocioId, Guid ServicioId, Guid EmpleadoId, Guid Cliente1, Guid Cliente2, Guid Dueno);

    private sealed class RelojFijo : TimeProvider
    {
        private DateTimeOffset _ahora;

        public RelojFijo(DateTimeOffset ahora) => _ahora = ahora;

        public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;

        public override DateTimeOffset GetUtcNow() => _ahora;
    }
}
