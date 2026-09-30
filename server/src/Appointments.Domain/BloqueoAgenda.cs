namespace Appointments.Domain;

public class BloqueoAgenda
{
    public Guid Id { get; set; }
    public Guid EmpleadoId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public string? Motivo { get; set; }

    public Empleado Empleado { get; set; } = null!;
}
