using System.Globalization;

namespace DhaqaleysteApp.Views;

/// <summary>True when the bound text is not empty (used to hide empty rows).</summary>
public class NotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? !string.IsNullOrWhiteSpace(text) : value != null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
