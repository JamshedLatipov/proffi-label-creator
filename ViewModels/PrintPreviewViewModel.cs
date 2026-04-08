using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;
using Microsoft.Win32;

namespace LabelStudio.ViewModels;

public partial class PrintPreviewViewModel : ViewModelBase
{
    private readonly EditorViewModel _editor;
    private readonly ISettingsService _settings;
    private readonly HttpClient _http = new();

    // ── Product lookup state ───────────────────────────────────────────
    /// <summary>Resolved dynamic-field values: key → value, e.g. "name" → "Widget Pro"</summary>
    private readonly Dictionary<string, string> _resolvedFields = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _articleInput = string.Empty;
    [ObservableProperty] private bool   _isLookingUp;

    // Autocomplete suggestions ("like" list shown when no exact match)
    [ObservableProperty] private bool _hasSuggestions;
    public ObservableCollection<ProductSuggestion> Suggestions { get; } = [];

    // found product display
    [ObservableProperty] private bool   _hasProduct;
    [ObservableProperty] private string _productName     = string.Empty;
    [ObservableProperty] private string _productArticle  = string.Empty;
    [ObservableProperty] private string _productPrice    = string.Empty;
    [ObservableProperty] private string _productCategory = string.Empty;
    [ObservableProperty] private string _lookupError     = string.Empty;

    partial void OnArticleInputChanged(string value)
    {
        // Clear stale state when user starts typing again
        HasProduct     = false;
        HasSuggestions = false;
        LookupError    = string.Empty;
        Suggestions.Clear();
    }

    // ── Print state ────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _printers = [];
    [ObservableProperty] private string? _selectedPrinter;
    [ObservableProperty] private int    _copies = 1;
    [ObservableProperty] private bool   _isPrinting;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private double _marginMm = 0;

    public EditorViewModel Editor => _editor;

    private const double MmToPx = 3.7795;

    partial void OnMarginMmChanged(double v)
    {
        OnPropertyChanged(nameof(MarginMmLabel));
        OnPropertyChanged(nameof(MarginPx));
        OnPropertyChanged(nameof(PreviewSafeWidth));
        OnPropertyChanged(nameof(PreviewSafeHeight));
        OnPropertyChanged(nameof(HasMargin));
        OnPropertyChanged(nameof(ShowMarginGuide));
    }

    public string MarginMmLabel    => $"{_marginMm:0.#}mm";
    public double MarginPx         => _marginMm * MmToPx;
    public double PreviewSafeWidth  => Math.Max(0, Editor.CanvasWidth  - 2 * MarginPx);
    public double PreviewSafeHeight => Math.Max(0, Editor.CanvasHeight - 2 * MarginPx);
    public bool   HasMargin        => _marginMm > 0;
    /// <summary>Only true when the safe-area rectangle has positive dimensions.
    /// Guards against a Skia AccessViolationException from dashed-stroke on a zero-size shape.</summary>
    public bool   ShowMarginGuide  => HasMargin && PreviewSafeWidth > 1 && PreviewSafeHeight > 1;

    public PrintPreviewViewModel(EditorViewModel editor, ISettingsService settings)
    {
        _editor   = editor;
        _settings = settings;
        LoadPrinters();
    }

    // ── Product lookup ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task LookupProductAsync()
    {
        if (string.IsNullOrWhiteSpace(ArticleInput)) return;

        LookupError    = string.Empty;
        HasProduct     = false;
        HasSuggestions = false;
        Suggestions.Clear();
        IsLookingUp    = true;
        _resolvedFields.Clear();

        try
        {
            var baseUrl = _settings.BackendUrl.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                LookupError = "Backend URL not configured. Open Connection Settings.";
                return;
            }

            var url = $"{baseUrl}/products/autocomplete/?q={Uri.EscapeDataString(ArticleInput.Trim())}&page=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(_settings.AuthToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AuthToken);

            var response = await _http.SendAsync(request);
            var rawBody  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                LookupError = $"Server returned {(int)response.StatusCode}";
                return;
            }

            using var doc  = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("body", out var bodyEl) ||
                bodyEl.ValueKind != JsonValueKind.Object)
            {
                LookupError = "Product not found";
                return;
            }

            // ── Exact match → apply immediately ──────────────────────
            if (bodyEl.TryGetProperty("match", out var matchEl) &&
                matchEl.ValueKind == JsonValueKind.Object)
            {
                var id   = matchEl.TryGetProperty("id",   out var mid)  ? mid.GetString()  ?? "" : "";
                var name = matchEl.TryGetProperty("name", out var mname) ? mname.GetString() ?? "" : "";
                ApplyProduct(id, name);
                return;
            }

            // ── No exact match → show "like" suggestions ─────────────
            if (bodyEl.TryGetProperty("like", out var likeEl) &&
                likeEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in likeEl.EnumerateArray())
                {
                    var id   = item.TryGetProperty("id",   out var sid)   ? sid.GetString()   ?? "" : "";
                    var name = item.TryGetProperty("name", out var sname)  ? sname.GetString()  ?? "" : "";
                    if (!string.IsNullOrEmpty(id))
                        Suggestions.Add(new ProductSuggestion(id, name));
                }

                if (Suggestions.Count > 0)
                    HasSuggestions = true;
                else
                    LookupError = "No products found";
            }
            else
            {
                LookupError = "No products found";
            }
        }
        catch (HttpRequestException ex)
        {
            LookupError = $"Network error: {ex.Message}";
        }
        catch (JsonException)
        {
            LookupError = "Failed to parse server response";
        }
        catch (Exception ex)
        {
            LookupError = $"Error: {ex.Message}";
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    [RelayCommand]
    private void SelectSuggestion(ProductSuggestion suggestion)
    {
        ApplyProduct(suggestion.Id, suggestion.Name);
        HasSuggestions = false;
        Suggestions.Clear();
    }

    private void ApplyProduct(string id, string name)
    {
        _resolvedFields["name"]         = name;
        _resolvedFields["product_name"] = name;
        _resolvedFields["id"]           = id;
        _resolvedFields["barcode"]      = ArticleInput.Trim();

        ProductName     = name;
        ProductArticle  = id;
        ProductPrice    = string.Empty;
        ProductCategory = string.Empty;
        HasProduct      = true;
    }

    /// <summary>Resolves a label element's display content, substituting dynamic field keys.</summary>
    private string ResolveContent(LabelElement m)
    {
        if (m.Kind != ElementKind.DynamicField || _resolvedFields.Count == 0)
            return m.Content;
        return _resolvedFields.TryGetValue(m.Content, out var val) ? val : m.Content;
    }

    private void LoadPrinters()
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers");
        var names = key?.GetSubKeyNames() ?? [];
        Printers = new ObservableCollection<string>(names);
        SelectedPrinter = Printers.Count > 0 ? Printers[0] : null;
    }

    [RelayCommand]
    private void IncreaseCopies() => Copies = Math.Min(999, Copies + 1);

    [RelayCommand]
    private void DecreaseCopies() => Copies = Math.Max(1, Copies - 1);

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private void Print()
    {
        if (SelectedPrinter is null) return;
        IsPrinting = true;
        try
        {
            // ── Step 1: read the printer's actual DPI from PrinterSettings,
            //   not from Graphics.DpiX which is unreliable on cheap thermal drivers.
            float dpiX, dpiY;
            using (var probe = new PrintDocument())
            {
                probe.PrinterSettings.PrinterName = SelectedPrinter;
                var res = probe.PrinterSettings.DefaultPageSettings.PrinterResolution;
                dpiX = res.X > 10 ? res.X : 203f;   // 203 dpi = standard thermal default
                dpiY = res.Y > 10 ? res.Y : 203f;
            }

            int bmpW = (int)Math.Round(_editor.LabelWidth  * dpiX / 25.4);
            int bmpH = (int)Math.Round(_editor.LabelHeight * dpiY / 25.4);
            StatusMessage = $"Rendering {bmpW}×{bmpH}px @ {dpiX}dpi · {_editor.Elements.Count} element(s) → {SelectedPrinter}";

            // ── Step 2: pre-render the entire label to an in-memory bitmap at printer DPI.
            //   This bypasses all GraphicsUnit conversion issues — DrawImage is the most
            //   basic GDI operation and every driver handles it without surprises.
            using var labelBmp = RenderToBitmap(bmpW, bmpH, dpiX, dpiY);

            // ── Step 3: paper size (hundredths of inch = mm / 25.4 * 100).
            //   RawKind=256 = user-defined — prevents the driver substituting its own default.
            int wH = (int)Math.Round(_editor.LabelWidth  / 25.4 * 100);
            int hH = (int)Math.Round(_editor.LabelHeight / 25.4 * 100);
            var paperSize = new PaperSize("Label", wH, hH) { RawKind = 256 };

            // ── Step 4: one job per copy — thermal drivers error when HasMorePages > 1.
            for (int copy = 0; copy < Copies; copy++)
            {
                using var doc = new PrintDocument();
                doc.DocumentName = _editor.ProjectName;
                doc.PrinterSettings.PrinterName = SelectedPrinter;
                doc.DefaultPageSettings.PaperSize = paperSize;
                doc.DefaultPageSettings.Margins   = new Margins(0, 0, 0, 0);

                var bmp    = labelBmp;        // local for lambda capture
                var _dpiX  = dpiX;
                var _dpiY  = dpiY;
                var _mgnPx = (float)(_marginMm * dpiX / 25.4f);
                doc.PrintPage += (_, e) =>
                {
                    e.Graphics!.PageUnit = GraphicsUnit.Pixel;

                    // Compensate for printer hard margin (non-printable zone).
                    float hmX = (float)(e.PageSettings.HardMarginX / 100.0 * _dpiX);
                    float hmY = (float)(e.PageSettings.HardMarginY / 100.0 * _dpiY);
                    e.Graphics.TranslateTransform(-hmX, -hmY);
                    e.Graphics.ResetClip();

                    // Apply user-defined margin offset (shifts content away from edge).
                    e.Graphics.DrawImage(bmp, _mgnPx, _mgnPx, bmpW, bmpH);
                    e.HasMorePages = false;
                };
                doc.Print();
            }

            StatusMessage = $"Done — {Copies} label(s) sent to {SelectedPrinter}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private bool CanPrint() => SelectedPrinter is not null && !IsPrinting;

    partial void OnSelectedPrinterChanged(string? value) => PrintCommand.NotifyCanExecuteChanged();
    partial void OnIsPrintingChanged(bool value)         => PrintCommand.NotifyCanExecuteChanged();

    // ── Bitmap renderer ──────────────────────────────────────────────────

    private Bitmap RenderToBitmap(int w, int h, float dpiX, float dpiY)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppRgb);
        bmp.SetResolution(dpiX, dpiY);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // All coordinates are in pixels. Scale factor: px = mm * dpi / 25.4
        float sx = dpiX / 25.4f;
        float sy = dpiY / 25.4f;

        foreach (var el in _editor.Elements)
        {
            var m = el.Model;
            float x  = (float)m.X      * sx,  y  = (float)m.Y      * sy;
            float mw = (float)m.Width   * sx,  mh = (float)m.Height * sy;

            switch (m.Kind)
            {
                case ElementKind.Text:
                case ElementKind.DynamicField:
                    BmpDrawText(g, m, x, y, mw, mh, dpiX, resolvedText: ResolveContent(m));
                    break;
                case ElementKind.Barcode:
                    BmpDrawBarcode(g, m, x, y, mw, mh);
                    break;
                case ElementKind.QrCode:
                    BmpDrawQr(g, m, x, y, mw, mh);
                    break;
                case ElementKind.Rectangle:
                    using (var pen = new Pen(Color.Black, Math.Max(1f, 0.3f * sx)))
                        g.DrawRectangle(pen, x, y, mw, mh);
                    break;
                case ElementKind.Image:
                    if (!string.IsNullOrEmpty(m.ImagePath) && System.IO.File.Exists(m.ImagePath))
                        using (var img = System.Drawing.Image.FromFile(m.ImagePath))
                            g.DrawImage(img, x, y, mw, mh);
                    break;
            }
        }

        return bmp;
    }

    private static void BmpDrawText(Graphics g, LabelElement m,
                                    float x, float y, float w, float h, float dpiX,
                                    string? resolvedText = null)
    {
        // Background fill
        if (!string.IsNullOrEmpty(m.TextBackground))
        {
            try
            {
                using var bgBrush = new SolidBrush(ColorTranslator.FromHtml(m.TextBackground));
                g.FillRectangle(bgBrush, x, y, w, h);
            }
            catch { /* ignore invalid color */ }
        }

        var style = (m.Bold          ? FontStyle.Bold      : FontStyle.Regular)
                  | (m.Italic        ? FontStyle.Italic     : FontStyle.Regular)
                  | (m.Underline     ? FontStyle.Underline  : FontStyle.Regular)
                  | (m.Strikethrough ? FontStyle.Strikeout  : FontStyle.Regular);
        var family = m.FontFamily.Split(',')[0].Trim();
        // Avalonia FontSize is in DIPs (96 per inch), not typographic points (72 per inch).
        // px = DIPs * dpi / 96
        float fontPx = (float)(m.FontSize * dpiX / 96f);
        using var font  = new Font(family, Math.Max(1f, fontPx), style, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(ColorTranslator.FromHtml(m.Color));
        // No trimming — let GDI+ wrap the text exactly as Avalonia TextWrapping="Wrap" does.
        var sf = new StringFormat { Trimming = StringTrimming.None };
        g.DrawString(resolvedText ?? m.Content, font, brush, new RectangleF(x, y, w, h), sf);
    }

    private static void BmpDrawBarcode(Graphics g, LabelElement m,
                                        float x, float y, float w, float h)
    {
        try
        {
            string val   = string.IsNullOrWhiteSpace(m.BarcodeValue) ? "0000000000000" : m.BarcodeValue;
            float  textH = Math.Max(8f, h * 0.18f);
            float  barcH = h - textH;

            var writer = new BarcodeWriterPixelData
            {
                Format  = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width  = (int)Math.Max(1, w),
                    Height = (int)Math.Max(1, barcH),
                    Margin = 0,
                    PureBarcode = true
                }
            };
            var pd  = writer.Write(val);
            using var bmp = PixelDataToBitmap(pd);
            g.DrawImage(bmp, x, y, w, barcH);

            // Human-readable digits below the bars.
            float fontSize = Math.Max(6f, textH * 0.7f);
            using var font = new Font("Courier New", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            var sf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.None };
            g.DrawString(val, font, Brushes.Black, new RectangleF(x, y + barcH, w, textH), sf);
        }
        catch
        {
            g.FillRectangle(Brushes.LightGray, x, y, w, h);
        }
    }

    private static void BmpDrawQr(Graphics g, LabelElement m,
                                   float x, float y, float w, float h)
    {
        try
        {
            int sz = (int)Math.Max(1, Math.Min(w, h));
            var writer = new BarcodeWriterPixelData
            {
                Format  = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width  = sz,
                    Height = sz,
                    Margin = 0
                }
            };
            var pd  = writer.Write(string.IsNullOrWhiteSpace(m.Content) ? "?" : m.Content);
            using var bmp = PixelDataToBitmap(pd);
            g.DrawImage(bmp, x, y, w, h);
        }
        catch
        {
            g.FillRectangle(Brushes.LightGray, x, y, w, h);
        }
    }

    private static Bitmap PixelDataToBitmap(PixelData pd)
    {
        var bmp   = new Bitmap(pd.Width, pd.Height, PixelFormat.Format32bppArgb);
        var bdata = bmp.LockBits(new Rectangle(0, 0, pd.Width, pd.Height),
                                  ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
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
}

/// <summary>One item in the autocomplete "like" list.</summary>
public sealed class ProductSuggestion
{
    public string Id   { get; }
    public string Name { get; }
    public ProductSuggestion(string id, string name) { Id = id; Name = name; }
    public override string ToString() => Name;
}
