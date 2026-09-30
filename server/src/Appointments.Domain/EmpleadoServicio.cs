namespace Appointments.Domain;

public class EmpleadoServicio
{
    public Guid EmpleadoId { get; set; }
    public Guid ServicioId { get; set; }
    public Empleado Empleado { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
}
