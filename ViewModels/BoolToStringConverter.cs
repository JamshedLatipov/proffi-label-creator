using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LabelStudio.ViewModels;

/// <summary>
/// Converts bool to one of two strings.
/// Use the static factory instances for common cases.
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    private readonly string _trueValue;
    private readonly string _falseValue;

    public BoolToStringConverter(string trueValue, string falseValue)
    {
        _trueValue = trueValue;
        _falseValue = falseValue;
    }

    /// <summary>true → "Mono", false → "Color"</summary>
    public static readonly BoolToStringConverter MonoColor = new("Mono", "Color");

    /// <summary>true → "Edit Profile", false → "Add Profile"</summary>
    public static readonly BoolToStringConverter EditAdd = new("Edit Profile", "Add Profile");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? _trueValue : _falseValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
