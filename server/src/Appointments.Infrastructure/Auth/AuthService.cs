using Appointments.Application;
using Appointments.Application.Auth;
using Appointments.Infrastructure.Identity;
using Appointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Appointments.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _users;
    private readonly AppDbContext _db;
    private readonly IEmisorDeTokens _tokens;
    private readonly TimeProvider _clock;
    private readonly int _refreshDays;

    public AuthService(
        UserManager<Usuario> users,
        AppDbContext db,
        IEmisorDeTokens tokens,
        TimeProvider clock,
        IConfiguration configuration)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
        _clock = clock;
        _refreshDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) ? days : 14;
    }

    public async Task<SesionEmitida> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken)
    {
        ValidarRegistro(request);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
            throw new ReglaDeNegocioException(409, "Cuenta existente", "Ese email ya está registrado.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Nombre = request.Nombre.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            CreadoUtc = _clock.GetUtcNow().UtcDateTime
        };

        var result = await _users.CreateAsync(usuario, request.Password);
        if (!result.Succeeded)
            throw new ReglaDeNegocioException(400, "Datos inválidos", string.Join(" ", result.Errors.Select(e => e.Description)));

        return await EmitirAsync(usuario, cancellationToken);
    }

    public async Task<SesionEmitida> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _users.FindByEmailAsync(email);
        if (usuario is null || !await _users.CheckPasswordAsync(usuario, request.Password))
            throw new ReglaDeNegocioException(401, "Credenciales", "Email o contraseña incorrectos.");

        if (await _users.IsLockedOutAsync(usuario))
            throw new ReglaDeNegocioException(401, "Cuenta bloqueada", "Demasiados intentos. Probá más tarde.");

        return await EmitirAsync(usuario, cancellationToken);
    }

    public async Task<SesionEmitida> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokens.Hash(refreshToken);
        var guardado = await _db.RefreshTokens.Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (guardado is null)
            throw new ReglaDeNegocioException(401, "Sesión", "La sesión no es válida.");

        if (guardado.RevocadoUtc is not null)
        {
            await RevocarTodosAsync(guardado.UsuarioId, cancellationToken);
            throw new ReglaDeNegocioException(401, "Sesión", "La sesión no es válida.");
        }

        if (guardado.ExpiraUtc <= _clock.GetUtcNow().UtcDateTime)
            throw new ReglaDeNegocioException(401, "Sesión", "La sesión expiró.");

        var nuevo = _tokens.EmitirRefresh();
        guardado.RevocadoUtc = _clock.GetUtcNow().UtcDateTime;
        guardado.ReemplazadoPorHash = _tokens.Hash(nuevo);
        await _db.SaveChangesAsync(cancellationToken);

        return await EmitirAsync(guardado.Usuario, cancellationToken, nuevo);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = _tokens.Hash(refreshToken);
        var guardado = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (guardado is null || guardado.RevocadoUtc is not null)
            return;

        guardado.RevocadoUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UsuarioDto> YoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _users.FindByIdAsync(usuarioId.ToString())
            ?? throw new ReglaDeNegocioException(401, "Sesión", "No encontramos la cuenta.");
        return ADto(usuario);
    }

    private async Task<SesionEmitida> EmitirAsync(Usuario usuario, CancellationToken cancellationToken, string? refresh = null)
    {
        var (access, expira) = _tokens.EmitirAccess(usuario.Id, usuario.Email!, usuario.Nombre);
        refresh ??= _tokens.EmitirRefresh();
        var ahora = _clock.GetUtcNow().UtcDateTime;

        if (!await _db.RefreshTokens.AnyAsync(x => x.TokenHash == _tokens.Hash(refresh), cancellationToken))
        {
            _db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                TokenHash = _tokens.Hash(refresh),
                CreadoUtc = ahora,
                ExpiraUtc = ahora.AddDays(_refreshDays)
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new SesionEmitida(new SesionDto(access, expira, ADto(usuario)), refresh);
    }

    private async Task RevocarTodosAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var ahora = _clock.GetUtcNow().UtcDateTime;
        var activos = await _db.RefreshTokens
            .Where(x => x.UsuarioId == usuarioId && x.RevocadoUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activos)
            token.RevocadoUtc = ahora;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UsuarioDto ADto(Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Email!, usuario.PhoneNumber);

    private static void ValidarRegistro(RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length < 2)
            throw new ReglaDeNegocioException(400, "Datos inválidos", "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ReglaDeNegocioException(400, "Datos inválidos", "El email no es válido.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ReglaDeNegocioException(400, "Datos inválidos", "La contraseña necesita al menos 8 caracteres.");
    }
}
