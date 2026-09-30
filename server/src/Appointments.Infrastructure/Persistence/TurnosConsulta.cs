using Appointments.Application.Catalogo;
using Appointments.Domain;
using Appointments.Infrastructure.Identity;

namespace Appointments.Infrastructure.Persistence;

internal static class TurnosConsulta
{
    public static IQueryable<TurnoListaDto> ALista(this IQueryable<Turno> turnos, IQueryable<Usuario> usuarios) =>
        from t in turnos
        join u in usuarios on t.ClienteId equals u.Id into clientes
        from u in clientes.DefaultIfEmpty()
        select new TurnoListaDto(
            t.Id,
            t.NegocioId,
            t.ServicioId,
            t.EmpleadoId,
            t.Negocio.Slug,
            t.Negocio.Nombre,
            t.Servicio.Nombre,
            t.Empleado.Nombre,
            u.Nombre ?? "",
            u.Email ?? "",
            t.InicioUtc,
            t.FinUtc,
            t.Estado);
}
