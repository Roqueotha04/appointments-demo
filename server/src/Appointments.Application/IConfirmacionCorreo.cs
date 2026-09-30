namespace Appointments.Application;

public interface IConfirmacionCorreo
{
    Task EnviarAsync(string para, string asunto, string cuerpo, CancellationToken cancellationToken);
}
