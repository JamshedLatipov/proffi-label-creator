using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LabelStudio.ViewModels;

/// <summary>Converts null → false, non-null → true. Used for IsVisible bindings.</summary>
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance  = new();

    /// <summary>Converts null or empty string → false, otherwise → true.</summary>
    public static readonly NullToBoolConverter IsNotEmpty = new NullToBoolConverter(checkEmpty: true);

    /// <summary>Converts null → true, non-null → false. Used for placeholder visibility.</summary>
    public static readonly NullToBoolConverter IsNull = new NullToBoolConverter(invert: true);

    private readonly bool _checkEmpty;
    private readonly bool _invert;
    private NullToBoolConverter(bool checkEmpty = false, bool invert = false)
    {
        _checkEmpty = checkEmpty;
        _invert     = invert;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool result;
        if (value is null)
            result = false;
        else if (_checkEmpty && value is string s)
            result = !string.IsNullOrWhiteSpace(s);
        else
            result = true;
        return _invert ? !result : result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
