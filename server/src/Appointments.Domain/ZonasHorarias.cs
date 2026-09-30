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

    public static DateTime AUtc(DateOnly fecha, TimeOnly hora, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified), zona);

    public static (DateTime DesdeUtc, DateTime HastaUtc) LimitesDelDiaUtc(DateOnly fecha, TimeZoneInfo zona) =>
        (AUtc(fecha, TimeOnly.MinValue, zona), AUtc(fecha.AddDays(1), TimeOnly.MinValue, zona));
}
