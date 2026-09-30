using Appointments.Domain;

namespace Appointments.Domain.Tests;

public class ZonasHorariasTests
{
    [Fact]
    public void Un_dia_sin_cambio_de_hora_dura_24_horas()
    {
        var zona = ZonasHorarias.Obtener("America/Argentina/Buenos_Aires");
        var (desde, hasta) = ZonasHorarias.LimitesDelDiaUtc(new DateOnly(2026, 9, 28), zona);

        Assert.Equal(TimeSpan.FromHours(24), hasta - desde);
    }

    [Fact]
    public void El_dia_en_que_adelantan_la_hora_dura_23()
    {
        var zona = ZonasHorarias.Obtener("America/New_York");
        var (desde, hasta) = ZonasHorarias.LimitesDelDiaUtc(new DateOnly(2026, 3, 8), zona);

        Assert.Equal(TimeSpan.FromHours(23), hasta - desde);
    }

    [Fact]
    public void El_dia_en_que_atrasan_la_hora_dura_25()
    {
        var zona = ZonasHorarias.Obtener("America/New_York");
        var (desde, hasta) = ZonasHorarias.LimitesDelDiaUtc(new DateOnly(2026, 11, 1), zona);

        Assert.Equal(TimeSpan.FromHours(25), hasta - desde);
    }
}
