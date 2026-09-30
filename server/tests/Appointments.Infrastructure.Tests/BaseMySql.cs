using Appointments.Application;
using Appointments.Application.Auth;
using Appointments.Application.Reservas;
using Appointments.Infrastructure.Auth;
using Appointments.Infrastructure.Identity;
using Appointments.Infrastructure.Persistence;
using Appointments.Infrastructure.Reservas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Appointments.Infrastructure.Tests;

public sealed class BaseMySql : IAsyncLifetime
{
    public const string BaseDePrueba = "appointments_test";

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        ConnectionString = CadenaDePrueba();
        await AsegurarBaseAsync(ConnectionString);
        await using var servicios = Crear(ConnectionString, TimeProvider.System);
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static ServiceProvider Crear(string connectionString, TimeProvider reloj)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "clave-de-prueba-con-mas-de-32-caracteres",
                ["Jwt:Issuer"] = "turnos",
                ["Jwt:Audience"] = "turnos-app",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14"
            })
            .Build());
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddIdentityCore<Usuario>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddSingleton(reloj);
        services.AddSingleton<IConfirmacionCorreo, CorreoNulo>();
        services.AddScoped<IEmisorDeTokens, JwtEmisor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReservasService, ReservasService>();
        return services.BuildServiceProvider();
    }

    private static string CadenaDePrueba()
    {
        var explicita = Environment.GetEnvironmentVariable("APPOINTMENTS_TEST_CONNECTION");
        var origen = string.IsNullOrWhiteSpace(explicita)
            ? new ConfigurationBuilder()
                .AddUserSecrets("c3a1e8f4-6b27-4d5a-9e10-2f6a0b8d4c71")
                .Build()
                .GetConnectionString("Default")
            : explicita;

        if (string.IsNullOrWhiteSpace(origen))
            throw new InvalidOperationException("Falta ConnectionStrings:Default en user secrets para correr los tests de MySQL.");

        var cadena = new MySqlConnectionStringBuilder(origen) { Database = BaseDePrueba };
        return cadena.ConnectionString;
    }

    private static async Task AsegurarBaseAsync(string connectionString)
    {
        var admin = new MySqlConnectionStringBuilder(connectionString) { Database = "" };
        await using var conexion = new MySqlConnection(admin.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = $"CREATE DATABASE IF NOT EXISTS `{BaseDePrueba}` CHARACTER SET utf8mb4";
        await comando.ExecuteNonQueryAsync();
    }

    private sealed class CorreoNulo : IConfirmacionCorreo
    {
        public Task EnviarAsync(string para, string asunto, string cuerpo, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionMySql : ICollectionFixture<BaseMySql>
{
    public const string Nombre = "mysql";
}
