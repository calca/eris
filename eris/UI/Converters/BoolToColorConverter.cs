using System.Globalization;

namespace eris.UI.Converters;

/// <summary>
/// Converte un boolean in un colore.
/// TrueColor e FalseColor sono configurabili in XAML.
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public Color TrueColor  { get; set; } = Colors.Green;
    public Color FalseColor { get; set; } = Colors.Gray;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueColor : FalseColor;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
