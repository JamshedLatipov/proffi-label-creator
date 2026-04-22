using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LabelStudio.ViewModels;

/// <summary>Converts null → false, non-null → true. Used for IsVisible bindings.</summary>
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
