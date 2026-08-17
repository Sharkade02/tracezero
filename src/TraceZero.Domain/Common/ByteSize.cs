using System.Globalization;

namespace TraceZero.Domain.Common;

/// <summary>
/// Formatage lisible des tailles en octets avec des unités binaires internationales (B, KB, MB, GB, TB)
/// — universellement comprises. Le séparateur décimal suit la culture active (ex. « 6.71 GB » en anglais,
/// « 6,71 GB » en français), pour rester cohérent avec la langue de l'interface.
/// </summary>
public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

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
        return string.Format(CultureInfo.CurrentCulture, "{0:N" + decimals + "} {1}", value, Units[unit]);
    }
}
