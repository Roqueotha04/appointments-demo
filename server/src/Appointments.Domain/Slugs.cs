using System.Globalization;
using System.Text;

namespace Appointments.Domain;

public static class Slugs
{
    public static string Desde(string nombre)
    {
        var normalizado = nombre.Trim().Normalize(NormalizationForm.FormD);
        var salida = new StringBuilder();

        foreach (var caracter in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(caracter))
                salida.Append(char.ToLowerInvariant(caracter));
            else if (salida.Length > 0 && salida[^1] != '-')
                salida.Append('-');
        }

        return salida.ToString().Trim('-');
    }
}
