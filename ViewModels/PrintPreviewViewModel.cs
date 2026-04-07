using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using Microsoft.Win32;

namespace LabelStudio.ViewModels;

public partial class PrintPreviewViewModel : ViewModelBase
{
    private readonly EditorViewModel _editor;

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
    }

    public string MarginMmLabel    => $"{_marginMm:0.#}mm";
    public double MarginPx         => _marginMm * MmToPx;
    public double PreviewSafeWidth  => Math.Max(0, Editor.CanvasWidth  - 2 * MarginPx);
    public double PreviewSafeHeight => Math.Max(0, Editor.CanvasHeight - 2 * MarginPx);
    public bool   HasMargin        => _marginMm > 0;

    public PrintPreviewViewModel(EditorViewModel editor)
    {
        _editor = editor;
        LoadPrinters();
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
                    BmpDrawText(g, m, x, y, mw, mh, dpiX);
                    break;
                case ElementKind.Barcode:
                    BmpDrawBarcode(g, m, x, y, mw, mh);
                    break;
                case ElementKind.QrCode:
                    BmpDrawQr(g, x, y, mw, mh);
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
                                    float x, float y, float w, float h, float dpiX)
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
        g.DrawString(m.Content, font, brush, new RectangleF(x, y, w, h), sf);
    }

    private static void BmpDrawBarcode(Graphics g, LabelElement m,
                                       float x, float y, float w, float h)
    {
        g.FillRectangle(Brushes.Black, x, y, w, h);
        float barH = h * 0.75f;
        float barW = w / 30f;
        for (int i = 1; i < 30; i += 2)
            g.FillRectangle(Brushes.White, x + i * barW, y + 1f, barW, barH);
        using var font = new Font("Courier New", Math.Max(1f, h * 0.2f),
                                  FontStyle.Regular, GraphicsUnit.Pixel);
        var sf = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString(m.BarcodeValue, font, Brushes.White,
                     new RectangleF(x, y + barH, w, h - barH), sf);
    }

    private static void BmpDrawQr(Graphics g, float x, float y, float w, float h)
    {
        g.FillRectangle(Brushes.Black, x, y, w, h);
        g.FillRectangle(Brushes.White, x + 2, y + 2, w - 4, h - 4);
        float sz = Math.Min(w, h) * 0.25f;
        g.FillRectangle(Brushes.Black, x + 2,          y + 2,          sz, sz);
        g.FillRectangle(Brushes.Black, x + w - sz - 2, y + 2,          sz, sz);
        g.FillRectangle(Brushes.Black, x + 2,          y + h - sz - 2, sz, sz);
    }
}
