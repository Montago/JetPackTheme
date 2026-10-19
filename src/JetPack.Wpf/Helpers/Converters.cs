using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JetPack.Wpf.Helpers;

/// <summary>Turns a <see cref="Color"/> into a frozen <see cref="SolidColorBrush"/>.</summary>
public sealed class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Color c)
        {
            return null;
        }
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        return brush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SolidColorBrush b ? b.Color : Binding.DoNothing;
}

/// <summary>Formats a <see cref="Color"/> as #RRGGBB (or #AARRGGBB when not opaque).</summary>
public sealed class ColorToHexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is Color c
        ? (c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}")
        : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            return value is string s ? (Color)ColorConverter.ConvertFromString(s) : Binding.DoNothing;
        }
        catch (FormatException)
        {
            return Binding.DoNothing;
        }
    }
}
