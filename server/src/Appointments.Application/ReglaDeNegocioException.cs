namespace Appointments.Application;

public sealed class ReglaDeNegocioException : Exception
{
    public int Status { get; }
    public string Titulo { get; }

    public ReglaDeNegocioException(int status, string titulo, string mensaje) : base(mensaje)
    {
        Status = status;
        Titulo = titulo;
    }
}
