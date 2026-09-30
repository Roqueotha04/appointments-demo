using Appointments.Application;

namespace Appointments.Infrastructure.Mail;

public class LocalConfirmacionCorreo : IConfirmacionCorreo
{
    public async Task EnviarAsync(string para, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "local-mail"));
        if (!raiz.Contains($"{Path.DirectorySeparatorChar}server{Path.DirectorySeparatorChar}local-mail", StringComparison.OrdinalIgnoreCase))
            raiz = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "local-mail"));

        Directory.CreateDirectory(raiz);
        var nombre = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.txt";
        var texto = $"Para: {para}{Environment.NewLine}Asunto: {asunto}{Environment.NewLine}{Environment.NewLine}{cuerpo}";
        await File.WriteAllTextAsync(Path.Combine(raiz, nombre), texto, cancellationToken);
    }
}
