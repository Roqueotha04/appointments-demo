namespace Appointments.Domain;

public readonly record struct Intervalo(DateTime Inicio, DateTime Fin);

public static class CalculadoraDeHuecos
{
    public static bool SeSolapan(Intervalo a, Intervalo b) =>
        a.Inicio < b.Fin && b.Inicio < a.Fin;

    public static IReadOnlyList<DateTime> Calcular(
        IEnumerable<Intervalo> ventanas,
        IEnumerable<Intervalo> ocupados,
        int duracionMinutos)
    {
        if (duracionMinutos <= 0)
            return [];

        var ocupado = ocupados.ToArray();
        var huecos = new List<DateTime>();

        foreach (var ventana in ventanas.OrderBy(v => v.Inicio))
        {
            var cursor = ventana.Inicio;
            while (cursor.AddMinutes(duracionMinutos) <= ventana.Fin)
            {
                var candidato = new Intervalo(cursor, cursor.AddMinutes(duracionMinutos));
                if (!ocupado.Any(o => SeSolapan(candidato, o)))
                    huecos.Add(cursor);

                cursor = cursor.AddMinutes(duracionMinutos);
            }
        }

        return huecos;
    }
}
