using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TraceZero.App.Converters;

/// <summary>Vrai si la valeur (enum) correspond au paramètre. Sert à lier des RadioButtons à un enum.</summary>
public sealed class EnumMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString()?.Equals(parameter?.ToString(), StringComparison.Ordinal) ?? false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null && Enum.TryParse(targetType, parameter.ToString(), out var result))
        {
            return result;
        }

        return Binding.DoNothing;
    }
}
