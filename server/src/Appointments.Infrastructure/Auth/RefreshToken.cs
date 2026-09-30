using Appointments.Infrastructure.Identity;

namespace Appointments.Infrastructure.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime? RevocadoUtc { get; set; }
    public string? ReemplazadoPorHash { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
