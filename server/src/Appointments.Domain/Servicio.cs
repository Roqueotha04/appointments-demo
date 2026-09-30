namespace Appointments.Domain;

public class Servicio
{
    public Guid Id { get; set; }
    public Guid NegocioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public ICollection<EmpleadoServicio> Empleados { get; set; } = [];
}
