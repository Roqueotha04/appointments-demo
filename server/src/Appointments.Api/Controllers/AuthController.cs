using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Appointments.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Appointments.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("registro")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Registro(RegistroRequest request, CancellationToken cancellationToken)
    {
        var sesion = await _auth.RegistrarAsync(request, cancellationToken);
        CookieSesion.Escribir(Response, sesion.RefreshToken);
        return Ok(sesion.Sesion);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var sesion = await _auth.LoginAsync(request, cancellationToken);
        CookieSesion.Escribir(Response, sesion.RefreshToken);
        return Ok(sesion.Sesion);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookieSesion.Nombre, out var refresh))
            return Unauthorized();

        var sesion = await _auth.RefreshAsync(refresh, cancellationToken);
        CookieSesion.Escribir(Response, sesion.RefreshToken);
        return Ok(sesion.Sesion);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(CookieSesion.Nombre, out var refresh);
        await _auth.LogoutAsync(refresh, cancellationToken);
        CookieSesion.Borrar(Response);
        return NoContent();
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<IActionResult> Yo(CancellationToken cancellationToken) =>
        Ok(await _auth.YoAsync(UsuarioId(), cancellationToken));

    private Guid UsuarioId() => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
