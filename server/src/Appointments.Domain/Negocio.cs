namespace Appointments.Domain;

public class Negocio
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Nombre { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";
    public bool Activo { get; set; } = true;
    public DateTime CreadoUtc { get; set; }

    public ICollection<Servicio> Servicios { get; set; } = [];
    public ICollection<Empleado> Empleados { get; set; } = [];
    public ICollection<Turno> Turnos { get; set; } = [];
}
