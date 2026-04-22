using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LabelStudio.ViewModels;

/// <summary>
/// Converts a string property to bool for RadioButton IsChecked.
/// Usage: IsChecked="{Binding DataSource, Converter={x:Static vm:StringEqualConverter.Instance}, ConverterParameter=ERP}"
/// </summary>
public class StringEqualConverter : IValueConverter
{
    public static readonly StringEqualConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter?.ToString();
        return null; // do not push false-checks back
    }
}
