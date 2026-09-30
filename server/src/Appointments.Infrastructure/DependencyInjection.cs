using Appointments.Application;
using Appointments.Application.Auth;
using Appointments.Application.Catalogo;
using Appointments.Application.Reservas;
using Appointments.Infrastructure.Auth;
using Appointments.Infrastructure.Catalogo;
using Appointments.Infrastructure.Identity;
using Appointments.Infrastructure.Mail;
using Appointments.Infrastructure.Persistence;
using Appointments.Infrastructure.Reservas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Appointments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Falta ConnectionStrings:Default en user secrets.");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

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

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IEmisorDeTokens, JwtEmisor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IReservasService, ReservasService>();
        services.AddSingleton<IConfirmacionCorreo, LocalConfirmacionCorreo>();
        return services;
    }
}
