namespace Appointments.Domain;

public class Empleado
{
    public Guid Id { get; set; }
    public Guid NegocioId { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public ICollection<EmpleadoServicio> Servicios { get; set; } = [];
    public ICollection<DisponibilidadSemanal> Disponibilidad { get; set; } = [];
    public ICollection<BloqueoAgenda> Bloqueos { get; set; } = [];
}
