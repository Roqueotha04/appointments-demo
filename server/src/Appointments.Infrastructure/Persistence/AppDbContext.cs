using Appointments.Domain;
using Appointments.Infrastructure.Auth;
using Appointments.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Appointments.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<EmpleadoServicio> EmpleadoServicios => Set<EmpleadoServicio>();
    public DbSet<DisponibilidadSemanal> Disponibilidad => Set<DisponibilidadSemanal>();
    public DbSet<BloqueoAgenda> Bloqueos => Set<BloqueoAgenda>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Negocio>(e =>
        {
            e.ToTable("Negocios");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Slug).HasMaxLength(140);
            e.Property(x => x.ZonaHoraria).HasMaxLength(80);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Servicio>(e =>
        {
            e.ToTable("Servicios");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.Precio).HasPrecision(10, 2);
            e.HasOne(x => x.Negocio).WithMany(x => x.Servicios).HasForeignKey(x => x.NegocioId);
        });

        builder.Entity<Empleado>(e =>
        {
            e.ToTable("Empleados");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.HasOne(x => x.Negocio).WithMany(x => x.Empleados).HasForeignKey(x => x.NegocioId);
        });

        builder.Entity<EmpleadoServicio>(e =>
        {
            e.ToTable("EmpleadoServicios");
            e.HasKey(x => new { x.EmpleadoId, x.ServicioId });
            e.HasOne(x => x.Empleado).WithMany(x => x.Servicios).HasForeignKey(x => x.EmpleadoId);
            e.HasOne(x => x.Servicio).WithMany(x => x.Empleados).HasForeignKey(x => x.ServicioId);
        });

        builder.Entity<DisponibilidadSemanal>(e =>
        {
            e.ToTable("DisponibilidadSemanal");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.Empleado).WithMany(x => x.Disponibilidad).HasForeignKey(x => x.EmpleadoId);
        });

        builder.Entity<BloqueoAgenda>(e =>
        {
            e.ToTable("BloqueosAgenda");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Motivo).HasMaxLength(200);
            e.HasOne(x => x.Empleado).WithMany(x => x.Bloqueos).HasForeignKey(x => x.EmpleadoId);
        });

        builder.Entity<Turno>(e =>
        {
            e.ToTable("Turnos");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.EmpleadoId, x.InicioUtc });
            e.HasIndex(x => new { x.ClienteId, x.NegocioId, x.InicioUtc });
            e.HasOne(x => x.Negocio).WithMany(x => x.Turnos).HasForeignKey(x => x.NegocioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });
    }
}
