using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Appointments.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Appointments.Infrastructure.Auth;

public class JwtEmisor : IEmisorDeTokens
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _clock;

    public JwtEmisor(IConfiguration configuration, TimeProvider clock)
    {
        _configuration = configuration;
        _clock = clock;
    }

    public (string AccessToken, DateTime ExpiraUtc) EmitirAccess(Guid usuarioId, string email, string nombre)
    {
        var key = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Falta Jwt:SigningKey en user secrets, con al menos 32 caracteres.");

        var minutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var parsed) ? parsed : 15;
        var expira = _clock.GetUtcNow().UtcDateTime.AddMinutes(minutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("nombre", nombre)
            ],
            notBefore: _clock.GetUtcNow().UtcDateTime,
            expires: expira,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public string EmitirRefresh() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
