using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;

namespace LabelStudio.ViewModels;

/// <summary>Wraps a LabelElement for binding on the canvas.</summary>
public partial class ElementViewModel : ViewModelBase
{
    public LabelElement Model { get; }
    private readonly EditorViewModel _editor;

    // mm → pixels conversion (96 dpi screen, 1mm ≈ 3.7795 px)
    private const double MmToPx = 3.7795;

    // ── Canvas position / size (in pixels, for the Canvas.Left / Top setters) ──
    [ObservableProperty] private double _left;
    [ObservableProperty] private double _top;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;

    // ── Geometry in mm — bound to the properties panel TextBoxes ──────
    // Changing these automatically propagates to the px canvas properties.
    [ObservableProperty] private double _xMm;
    [ObservableProperty] private double _yMm;
    [ObservableProperty] private double _widthMm;
    [ObservableProperty] private double _heightMm;

    partial void OnXMmChanged(double v)      { Left   = v * MmToPx; Model.X      = v; _editor.MarkDirty(); }
    partial void OnYMmChanged(double v)      { Top    = v * MmToPx; Model.Y      = v; _editor.MarkDirty(); }
    partial void OnWidthMmChanged(double v)  { Width  = v * MmToPx; Model.Width  = v; _editor.MarkDirty(); }
    partial void OnHeightMmChanged(double v) { Height = v * MmToPx; Model.Height = v; _editor.MarkDirty(); }

    // ── Visual state ───────────────────────────────────────────────────
    [ObservableProperty] private bool _isSelected;

    // ── Text properties (bound to property panel) ──────────────────────
    [ObservableProperty] private string _content;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FontFamilyResolved))]
    private string _fontFamily;
    [ObservableProperty] private double _fontSize;
    [ObservableProperty] private bool   _bold;
    [ObservableProperty] private bool   _italic;
    [ObservableProperty] private string _color;
    [ObservableProperty] private string _barcodeValue;

    /// <summary>Resolves the stored font name to a proper FontFamily, including embedded avares:// fonts.</summary>
    public FontFamily FontFamilyResolved => _fontFamily switch
    {
        "Manrope" => new FontFamily("avares://LabelStudio/Assets/Fonts/Manrope.ttf#Manrope"),
        "Consolas, Monospace" or "Consolas,Monospace" => new FontFamily("Consolas,Monospace"),
        _ => new FontFamily(_fontFamily)
    };

    public ElementKind Kind => Model.Kind;

    public string KindLabel => Model.Kind switch
    {
        ElementKind.Text         => "Static Text",
        ElementKind.DynamicField => "Dynamic Field",
        ElementKind.Barcode      => "Barcode",
        ElementKind.QrCode       => "QR Code",
        ElementKind.Rectangle    => "Rectangle",
        ElementKind.Image        => "Image",
        _                        => "Element"
    };

    public string IconGlyph => Model.Kind switch
    {
        ElementKind.Text         => "\ue264",
        ElementKind.DynamicField => "\uf20e",
        ElementKind.Barcode      => "\ue85c",
        ElementKind.QrCode       => "\ue00a",
        ElementKind.Rectangle    => "\ueb54",
        ElementKind.Image        => "\ue3f4",
        _                        => "\ue3f4"
    };

    public ElementViewModel(LabelElement model, EditorViewModel editor)
    {
        Model        = model;
        _editor      = editor;
        _left        = model.X      * MmToPx;
        _top         = model.Y      * MmToPx;
        _width       = model.Width  * MmToPx;
        _height      = model.Height * MmToPx;
        _xMm         = model.X;
        _yMm         = model.Y;
        _widthMm     = model.Width;
        _heightMm    = model.Height;
        _content     = model.Content;
        _fontFamily  = model.FontFamily;
        _fontSize    = model.FontSize;
        _bold        = model.Bold;
        _italic      = model.Italic;
        _color       = model.Color;
        _barcodeValue = model.BarcodeValue;
    }

    // ── Sync px → mm back to model (called after drag) ─────────────────
    public void SyncPosition()
    {
        // Update mm properties without triggering cascading px recalc — set
        // backing fields directly and raise PropertyChanged manually.
        _xMm      = Left   / MmToPx;  OnPropertyChanged(nameof(XMm));
        _yMm      = Top    / MmToPx;  OnPropertyChanged(nameof(YMm));
        _widthMm  = Width  / MmToPx;  OnPropertyChanged(nameof(WidthMm));
        _heightMm = Height / MmToPx;  OnPropertyChanged(nameof(HeightMm));
        Model.X      = _xMm;
        Model.Y      = _yMm;
        Model.Width  = _widthMm;
        Model.Height = _heightMm;
        _editor.MarkDirty();
    }

    partial void OnContentChanged(string value)      { Model.Content     = value; _editor.MarkDirty(); }
    partial void OnFontFamilyChanged(string value)   { Model.FontFamily  = value; _editor.MarkDirty(); }
    partial void OnFontSizeChanged(double value)     { Model.FontSize    = value; _editor.MarkDirty(); }
    partial void OnBoldChanged(bool value)           { Model.Bold        = value; _editor.MarkDirty(); }
    partial void OnItalicChanged(bool value)         { Model.Italic      = value; _editor.MarkDirty(); }
    partial void OnColorChanged(string value)        { Model.Color       = value; _editor.MarkDirty(); }
    partial void OnBarcodeValueChanged(string value) { Model.BarcodeValue = value; _editor.MarkDirty(); }

    [RelayCommand]
    private void Select() => _editor.SelectElement(this);
}
