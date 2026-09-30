namespace Appointments.Domain;

public static class ZonasHorarias
{
    public static TimeZoneInfo Obtener(string id)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zona))
            return zona;

        if (id == "America/Argentina/Buenos_Aires"
            && TimeZoneInfo.TryFindSystemTimeZoneById("Argentina Standard Time", out zona))
            return zona;

        throw new TimeZoneNotFoundException($"No se reconoce la zona horaria {id}.");
    }
}
