using System.Globalization;

namespace TraceZero.Domain.Common;

/// <summary>Formatage lisible des tailles en octets, en français (o, Ko, Mo, Go, To).</summary>
public static class ByteSize
{
    private static readonly string[] Units = ["o", "Ko", "Mo", "Go", "To", "Po"];
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            bytes = 0;
        }

        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var decimals = unit == 0 ? 0 : value >= 100 ? 0 : value >= 10 ? 1 : 2;
        return string.Format(Fr, "{0:N" + decimals + "} {1}", value, Units[unit]);
    }
}
