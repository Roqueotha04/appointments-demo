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
        return new Escenario(negocio.Id, servicio.Id, empleado.Id, cliente1.Sesion.Usuario.Id, cliente2.Sesion.Usuario.Id);
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

    private sealed record Escenario(Guid NegocioId, Guid ServicioId, Guid EmpleadoId, Guid Cliente1, Guid Cliente2);

    private sealed class RelojFijo : TimeProvider
    {
        private readonly DateTimeOffset _ahora;

        public RelojFijo(DateTimeOffset ahora) => _ahora = ahora;

        public override DateTimeOffset GetUtcNow() => _ahora;
    }
}
