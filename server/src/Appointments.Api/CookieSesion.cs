namespace Appointments.Api;

public static class CookieSesion
{
    public const string Nombre = "turnos_refresh";

    public static void Escribir(HttpResponse response, string token)
    {
        response.Cookies.Append(Nombre, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        });
    }

    public static void Borrar(HttpResponse response) =>
        response.Cookies.Delete(Nombre, new CookieOptions { Path = "/api/auth" });
}
