using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

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
    partial void OnWidthMmChanged(double v)
    {
        Width  = v * MmToPx; Model.Width  = v; _editor.MarkDirty();
        RegenerateBarQr();
    }
    partial void OnHeightMmChanged(double v)
    {
        Height = v * MmToPx; Model.Height = v; _editor.MarkDirty();
        RegenerateBarQr();
    }

    private void RegenerateBarQr()
    {
        int w = Math.Max(1, (int)_width); int h = Math.Max(1, (int)_height);
        if (Model.Kind == ElementKind.Barcode)
        {
            _cachedBarcodeSource?.Dispose();
            _cachedBarcodeSource = RenderBarcode(_barcodeValue, BarcodeFormat.CODE_128, w, h);
            // Also re-render the preview override at the new size if active
            if (_cachedPreviewBarcodeSource is not null)
            {
                var previewVal = _cachedPreviewBarcodeSource; // grab to get the value — re-render via property setter
                _cachedPreviewBarcodeSource?.Dispose();
                _cachedPreviewBarcodeSource = null;
                // We don't store the preview string, so just clear it; caller can re-push
            }
            OnPropertyChanged(nameof(BarcodeSource));
        }
        else if (Model.Kind == ElementKind.QrCode)
        {
            _cachedQrSource?.Dispose();
            _cachedQrSource = RenderBarcode(_content, BarcodeFormat.QR_CODE, w, h);
            OnPropertyChanged(nameof(QrSource));
        }
    }

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextDecorationsList))]
    private bool _underline;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextDecorationsList))]
    private bool _strikethrough;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextBackgroundBrush))]
    private string _textBackground;
    [ObservableProperty] private string _color;
    [ObservableProperty] private string _barcodeValue;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageSource))]
    private string _imagePath;

    // Bitmap cached here so Skia never GC's the native backing while rendering.
    private Bitmap? _cachedImageSource;
    private Bitmap? _cachedBarcodeSource;
    private Bitmap? _cachedQrSource;
    private Bitmap? _cachedPreviewBarcodeSource;

    public Bitmap? ImageSource   => _cachedImageSource;
    public Bitmap? BarcodeSource => _cachedPreviewBarcodeSource ?? _cachedBarcodeSource;
    public Bitmap? QrSource      => _cachedQrSource;

    // ── Preview override (set by PrintPreviewViewModel after product lookup) ──
    private string? _previewValue;
    /// <summary>
    /// When set by the print-preview, dynamic-field elements display this instead of the raw key.
    /// Set to null to revert to showing the key.
    /// </summary>
    public string? PreviewValue
    {
        get => _previewValue;
        set { _previewValue = value; OnPropertyChanged(nameof(DisplayContent)); }
    }
    /// <summary>Resolved display text for the preview canvas. Returns PreviewValue when set, otherwise Content.</summary>
    public string DisplayContent => _previewValue ?? _content;

    /// <summary>
    /// When set, the preview canvas shows this barcode instead of the element's stored BarcodeValue.
    /// Set to null to revert.
    /// </summary>
    public string? PreviewBarcodeValue
    {
        set
        {
            _cachedPreviewBarcodeSource?.Dispose();
            _cachedPreviewBarcodeSource = value is not null
                ? RenderBarcode(value, BarcodeFormat.CODE_128, Math.Max(1, (int)_width), Math.Max(1, (int)_height))
                : null;
            OnPropertyChanged(nameof(BarcodeSource));
        }
    }

    private static Bitmap? LoadBitmap(string? path) =>
        !string.IsNullOrEmpty(path) && File.Exists(path) ? new Bitmap(path) : null;

    /// <summary>Renders a ZXing barcode/QR to an Avalonia Bitmap.
    /// Combined bitmap is exactly w×h so Stretch="Fill" gives 1:1 mapping.</summary>
    private static Bitmap? RenderBarcode(string value, BarcodeFormat format, int w, int h)
    {
        if (string.IsNullOrWhiteSpace(value) || w < 1 || h < 1) return null;
        try
        {
            bool isQr  = format == BarcodeFormat.QR_CODE;
            int  textH = isQr ? 0 : Math.Max(10, h / 5);
            int  barcH = h - textH;

            var writer = new BarcodeWriterPixelData
            {
                Format  = format,
                Options = new EncodingOptions { Width = w, Height = barcH, Margin = 0, PureBarcode = true }
            };
            var pd = writer.Write(value);

            // Always allocate combined at exactly w×h so Fill-stretch is 1:1.
            using var combined = new System.Drawing.Bitmap(w, h,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = System.Drawing.Graphics.FromImage(combined);
            g.Clear(System.Drawing.Color.White);

            using (var barBmp = PixelDataToSDBitmap(pd))
                g.DrawImage(barBmp, 0, 0, w, barcH);  // stretch bars to exact width

            if (textH > 0)
            {
                float fontSize = Math.Max(6f, textH * 0.65f);
                using var font = new System.Drawing.Font("Courier New", fontSize,
                    System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
                var sf = new System.Drawing.StringFormat
                    { Alignment = System.Drawing.StringAlignment.Center,
                      Trimming  = System.Drawing.StringTrimming.None };
                g.DrawString(value, font, System.Drawing.Brushes.Black,
                    new System.Drawing.RectangleF(0, barcH, w, textH), sf);
            }

            using var ms = new MemoryStream();
            combined.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch { return null; }
    }

    private static System.Drawing.Bitmap PixelDataToSDBitmap(PixelData pd)
    {
        var bmp   = new System.Drawing.Bitmap(pd.Width, pd.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var bdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, pd.Width, pd.Height),
                                  System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                  System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            // ZXing.Rendering.PixelData.Pixels may be byte[] or int[] depending on version.
            // Pattern-match at runtime to copy correctly either way.
            var raw = (object)pd.Pixels;
            if (raw is byte[] bytes)
                for (int row = 0; row < pd.Height; row++)
                    Marshal.Copy(bytes, row * pd.Width * 4,
                                 IntPtr.Add(bdata.Scan0, row * bdata.Stride), pd.Width * 4);
            else if (raw is int[] ints)
                for (int row = 0; row < pd.Height; row++)
                    Marshal.Copy(ints, row * pd.Width,
                                 IntPtr.Add(bdata.Scan0, row * bdata.Stride), pd.Width);
        }
        finally { bmp.UnlockBits(bdata); }
        return bmp;
    }

    /// <summary>Resolves the stored font name to a proper FontFamily, including embedded avares:// fonts.</summary>
    public TextDecorationCollection? TextDecorationsList
    {
        get
        {
            if (!_underline && !_strikethrough) return null;
            var list = new TextDecorationCollection();
            if (_underline)     foreach (var d in TextDecorations.Underline)     list.Add(d);
            if (_strikethrough) foreach (var d in TextDecorations.Strikethrough) list.Add(d);
            return list;
        }
    }

    public IBrush? TextBackgroundBrush =>
        string.IsNullOrEmpty(_textBackground)
            ? null
            : new SolidColorBrush(Avalonia.Media.Color.Parse(_textBackground));

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
        _bold           = model.Bold;
        _italic         = model.Italic;
        _underline      = model.Underline;
        _strikethrough  = model.Strikethrough;
        _textBackground = model.TextBackground;
        _color          = model.Color;
        _barcodeValue = model.BarcodeValue;
        _imagePath           = model.ImagePath;
        _cachedImageSource   = LoadBitmap(model.ImagePath);
        _cachedBarcodeSource = RenderBarcode(model.BarcodeValue, BarcodeFormat.CODE_128, (int)_width, (int)_height);
        _cachedQrSource      = RenderBarcode(model.Content, BarcodeFormat.QR_CODE, (int)_width, (int)_height);
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

    partial void OnContentChanged(string value)
    {
        Model.Content = value;
        _editor.MarkDirty();
        OnPropertyChanged(nameof(DisplayContent));
        // Refresh QR if this is a QR element
        if (Model.Kind == ElementKind.QrCode)
        {
            _cachedQrSource?.Dispose();
            _cachedQrSource = RenderBarcode(value, BarcodeFormat.QR_CODE, Math.Max(1,(int)_width), Math.Max(1,(int)_height));
            OnPropertyChanged(nameof(QrSource));
        }
    }
    partial void OnFontFamilyChanged(string value)   { Model.FontFamily  = value; _editor.MarkDirty(); }
    partial void OnFontSizeChanged(double value)     { Model.FontSize    = value; _editor.MarkDirty(); }
    partial void OnBoldChanged(bool value)              { Model.Bold           = value; _editor.MarkDirty(); }
    partial void OnItalicChanged(bool value)           { Model.Italic         = value; _editor.MarkDirty(); }
    partial void OnUnderlineChanged(bool value)        { Model.Underline      = value; _editor.MarkDirty(); }
    partial void OnStrikethroughChanged(bool value)    { Model.Strikethrough  = value; _editor.MarkDirty(); }
    partial void OnTextBackgroundChanged(string value) { Model.TextBackground = value; _editor.MarkDirty(); }
    partial void OnColorChanged(string value)          { Model.Color          = value; _editor.MarkDirty(); }
    partial void OnBarcodeValueChanged(string value)
    {
        Model.BarcodeValue = value;
        _editor.MarkDirty();
        _cachedBarcodeSource?.Dispose();
        _cachedBarcodeSource = RenderBarcode(value, BarcodeFormat.CODE_128, Math.Max(1,(int)_width), Math.Max(1,(int)_height));
        OnPropertyChanged(nameof(BarcodeSource));
    }
    partial void OnImagePathChanged(string value)
    {
        _cachedImageSource?.Dispose();
        _cachedImageSource = LoadBitmap(value);
        Model.ImagePath    = value;
        _editor.MarkDirty();
    }

    [RelayCommand]
    private void SetTextBackground(string? color) => TextBackground = color ?? string.Empty;

    [RelayCommand]
    private void Select() => _editor.SelectElement(this);
}
