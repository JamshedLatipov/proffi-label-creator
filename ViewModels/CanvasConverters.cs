using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LabelStudio.Models;

namespace LabelStudio.ViewModels;

// ── bool → Color for selection highlight ────────────────────────────────────
public class BoolToColorConverter : IValueConverter
{
    private readonly Color _trueColor;
    private readonly Color _falseColor;

    public static readonly BoolToColorConverter SelectionBorder =
        new(Color.Parse("#2563eb"), Colors.Transparent);

    public static readonly BoolToColorConverter SelectionFill =
        new(Color.Parse("#0d2563eb"), Colors.Transparent);

    public BoolToColorConverter(Color trueColor, Color falseColor)
    {
        _trueColor  = trueColor;
        _falseColor = falseColor;
    }

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? _trueColor : _falseColor;

    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── ElementKind → bool visibility converters ─────────────────────────────────
public class ElementKindConverter : IValueConverter
{
    private readonly Func<ElementKind, bool> _predicate;

    public static readonly ElementKindConverter IsText        = new(k => k == ElementKind.Text);
    public static readonly ElementKindConverter IsDynamic     = new(k => k == ElementKind.DynamicField);
    public static readonly ElementKindConverter IsBarcode     = new(k => k == ElementKind.Barcode);
    public static readonly ElementKindConverter IsQr          = new(k => k == ElementKind.QrCode);
    public static readonly ElementKindConverter IsRect        = new(k => k == ElementKind.Rectangle);
    public static readonly ElementKindConverter IsImage         = new(k => k == ElementKind.Image);
    public static readonly ElementKindConverter IsTextOrDynamic =
        new(k => k is ElementKind.Text or ElementKind.DynamicField);

    private ElementKindConverter(Func<ElementKind, bool> predicate) => _predicate = predicate;

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is ElementKind k && _predicate(k);

    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── bool → FontWeight ──────────────────────────────────────────────────────────
public class BoolToFontWeightConverter : IValueConverter
{
    public static readonly BoolToFontWeightConverter Instance = new();

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? FontWeight.Bold : FontWeight.Normal;

    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── bool → FontStyle ──────────────────────────────────────────────────────────
public class BoolToFontStyleConverter : IValueConverter
{
    public static readonly BoolToFontStyleConverter Instance = new();

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? FontStyle.Italic : FontStyle.Normal;

    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
