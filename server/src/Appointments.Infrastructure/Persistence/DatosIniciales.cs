using Appointments.Domain;
using Appointments.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Appointments.Infrastructure.Persistence;

public static class DatosIniciales
{
    public const string OwnerEmail = "owner@local.test";
    public const string ClienteEmail = "cliente@local.test";
    public const string Password = "Local-owner-123";
    public const string PasswordCliente = "Local-cliente-123";

    public static async Task CargarAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<Usuario>>();
        var db = services.GetRequiredService<AppDbContext>();
        if (await users.FindByEmailAsync(OwnerEmail) is not null)
            return;

        var owner = await Crear(users, OwnerEmail, "Estudio Norte", Password);
        var cliente = await Crear(users, ClienteEmail, "Ana Cliente", PasswordCliente);
        _ = cliente;

        var negocio = new Negocio
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            Nombre = "Estudio Norte",
            Slug = "estudio-norte",
            ZonaHoraria = "America/Argentina/Buenos_Aires",
            CreadoUtc = DateTime.UtcNow
        };
        var empleado = new Empleado { Id = Guid.NewGuid(), NegocioId = negocio.Id, Nombre = "Lucía" };
        var servicios = new[]
        {
            Nuevo(negocio.Id, "Corte caballero", "Corte clásico o moderno.", 30, 2500),
            Nuevo(negocio.Id, "Corte y barba", "Corte y perfilado de barba.", 60, 3500),
            Nuevo(negocio.Id, "Corte", "Corte, lavado y secado.", 60, 4500),
            Nuevo(negocio.Id, "Color", "Color e hidratación.", 90, 6000)
        };

        db.Negocios.Add(negocio);
        db.Empleados.Add(empleado);
        db.Servicios.AddRange(servicios);
        foreach (var servicio in servicios)
            db.EmpleadoServicios.Add(new EmpleadoServicio { EmpleadoId = empleado.Id, ServicioId = servicio.Id });

        for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Saturday; dia++)
        {
            db.Disponibilidad.Add(new DisponibilidadSemanal
            {
                Id = Guid.NewGuid(),
                EmpleadoId = empleado.Id,
                DiaSemana = dia,
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(18, 0)
            });
        }

        await db.SaveChangesAsync();
    }

    private static Servicio Nuevo(Guid negocioId, string nombre, string descripcion, int minutos, decimal precio) =>
        new()
        {
            Id = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = nombre,
            Descripcion = descripcion,
            DuracionMinutos = minutos,
            Precio = precio
        };

    private static async Task<Usuario> Crear(UserManager<Usuario> users, string email, string nombre, string password)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Nombre = nombre,
            EmailConfirmed = true,
            CreadoUtc = DateTime.UtcNow
        };
        var result = await users.CreateAsync(usuario, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        return usuario;
    }
}
