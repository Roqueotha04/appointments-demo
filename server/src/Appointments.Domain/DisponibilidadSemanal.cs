namespace Appointments.Domain;

public class DisponibilidadSemanal
{
    public Guid Id { get; set; }
    public Guid EmpleadoId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    public Empleado Empleado { get; set; } = null!;
}
