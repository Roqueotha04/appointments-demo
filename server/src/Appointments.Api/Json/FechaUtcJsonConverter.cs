using System.Text.Json;
using System.Text.Json.Serialization;

namespace Appointments.Api.Json;

public sealed class FechaUtcJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valor = reader.GetDateTime();
        return valor.Kind switch
        {
            DateTimeKind.Local => valor.ToUniversalTime(),
            DateTimeKind.Utc => valor,
            _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc);
    }
}
