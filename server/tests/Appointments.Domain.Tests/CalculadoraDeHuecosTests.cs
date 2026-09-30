using Appointments.Domain;

namespace Appointments.Domain.Tests;

public class CalculadoraDeHuecosTests
{
    private static DateTime Hora(int hora, int minuto = 0) => new(2026, 9, 30, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void Parte_la_ventana_segun_la_duracion()
    {
        var huecos = CalculadoraDeHuecos.Calcular(
            [new Intervalo(Hora(10), Hora(12))],
            [],
            60);

        Assert.Equal([Hora(10), Hora(11)], huecos);
    }

    [Fact]
    public void Saltea_el_hueco_ocupado()
    {
        var huecos = CalculadoraDeHuecos.Calcular(
            [new Intervalo(Hora(10), Hora(12))],
            [new Intervalo(Hora(10), Hora(11))],
            60);

        Assert.Equal([Hora(11)], huecos);
    }

    [Fact]
    public void Un_solape_parcial_anula_el_hueco()
    {
        var huecos = CalculadoraDeHuecos.Calcular(
            [new Intervalo(Hora(10), Hora(11))],
            [new Intervalo(Hora(10, 15), Hora(10, 45))],
            30);

        Assert.Empty(huecos);
    }
}
