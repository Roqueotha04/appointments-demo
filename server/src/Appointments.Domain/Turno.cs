namespace Appointments.Domain;

public class Turno
{
    public Guid Id { get; set; }
    public Guid NegocioId { get; set; }
    public Guid ServicioId { get; set; }
    public Guid EmpleadoId { get; set; }
    public Guid ClienteId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Confirmado;
    public DateTime CreadoUtc { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
    public Empleado Empleado { get; set; } = null!;
}
