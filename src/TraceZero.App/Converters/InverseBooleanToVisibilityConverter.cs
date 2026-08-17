using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TraceZero.App.Converters;

/// <summary>Inverse de <c>BooleanToVisibilityConverter</c> : vrai → Collapsed, faux → Visible (Phase 1).</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}
