namespace Appointments.Application.Auth;

public record RegistroRequest(string Nombre, string Email, string Password, string? Telefono);
public record LoginRequest(string Email, string Password);
public record UsuarioDto(Guid Id, string Nombre, string Email, string? Telefono);
public record SesionDto(string AccessToken, DateTime ExpiraUtc, UsuarioDto Usuario);
public record SesionEmitida(SesionDto Sesion, string RefreshToken);

public interface IAuthService
{
    Task<SesionEmitida> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken);
    Task<SesionEmitida> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<SesionEmitida> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);
    Task<UsuarioDto> YoAsync(Guid usuarioId, CancellationToken cancellationToken);
}

public interface IEmisorDeTokens
{
    (string AccessToken, DateTime ExpiraUtc) EmitirAccess(Guid usuarioId, string email, string nombre);
    string EmitirRefresh();
    string Hash(string token);
}
