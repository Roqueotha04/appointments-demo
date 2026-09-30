using Microsoft.AspNetCore.Identity;

namespace Appointments.Infrastructure.Identity;

public class Usuario : IdentityUser<Guid>
{
    public string Nombre { get; set; } = "";
    public DateTime CreadoUtc { get; set; }
}
