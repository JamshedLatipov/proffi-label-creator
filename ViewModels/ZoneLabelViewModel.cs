using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Services;
using Microsoft.Win32;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace LabelStudio.ViewModels;

public partial class ZoneLabelViewModel : ViewModelBase
{
    private readonly IWarehouseService _warehouseService;

    // ── Warehouses ─────────────────────────────────────────────────────
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private WarehouseDto? _selectedWarehouse;

    // ── Zones ──────────────────────────────────────────────────────────
    public ObservableCollection<ZoneDto> Zones { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private ZoneDto? _selectedZone;

    // ── Shelves ────────────────────────────────────────────────────────
    public ObservableCollection<ShelfDto> Shelves { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private ShelfDto? _selectedShelf;

    // ── Boxes ──────────────────────────────────────────────────────────
    public ObservableCollection<BoxDto> Boxes { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private BoxDto? _selectedBox;

    // ── Loading indicators ─────────────────────────────────────────────
    [ObservableProperty] private bool   _isLoadingWarehouses;
    [ObservableProperty] private bool   _isLoadingZones;
    [ObservableProperty] private bool   _isLoadingShelves;
    [ObservableProperty] private bool   _isLoadingBoxes;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // ── Printers ───────────────────────────────────────────────────────
    public ObservableCollection<string> SystemPrinters { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    private string? _selectedPrinter;

    // ── Label dimensions (mm) ──────────────────────────────────────────
    [ObservableProperty] private double _labelWidth  = 40;
    [ObservableProperty] private double _labelHeight = 40;

    // ── Copies ────────────────────────────────────────────────────────
    [ObservableProperty] private int _copies = 1;

    // ── QR preview ────────────────────────────────────────────────────
    private AvaloniaBitmap? _qrPreview;

    public AvaloniaBitmap? QrPreview
    {
        get => _qrPreview;
        private set
        {
            if (ReferenceEquals(_qrPreview, value)) return;
            var old = _qrPreview;
            _qrPreview = value;
            OnPropertyChanged();
            old?.Dispose();
        }
    }

    // ── State ─────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isPrinting;

    public ZoneLabelViewModel(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
        LoadSystemPrinters();
        _ = LoadWarehousesAsync();
    }

    private void LoadSystemPrinters()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        using var key = Registry.LocalMachine?.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers");
        foreach (var name in key?.GetSubKeyNames() ?? [])
            SystemPrinters.Add(name);

        SelectedPrinter = SystemPrinters.Count > 0 ? SystemPrinters[0] : null;
    }

    // ── Warehouse → Zones ──────────────────────────────────────────────
    private async System.Threading.Tasks.Task LoadWarehousesAsync()
    {
        IsLoadingWarehouses = true;
        StatusMessage = "Загрузка складов…";
        try
        {
            var list = await _warehouseService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in list) Warehouses.Add(w);
            StatusMessage = Warehouses.Count > 0 ? string.Empty : "Склады не найдены.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки складов: {ex.Message}";
        }
        finally
        {
            IsLoadingWarehouses = false;
        }
    }

    partial void OnSelectedWarehouseChanged(WarehouseDto? value)
    {
        Zones.Clear();
        Shelves.Clear();
        Boxes.Clear();
        SelectedZone  = null;
        SelectedShelf = null;
        SelectedBox   = null;

        if (value is null) return;
        _ = LoadZonesAsync(value.Id);
    }

    private async System.Threading.Tasks.Task LoadZonesAsync(string warehouseId)
    {
        IsLoadingZones = true;
        StatusMessage = "Загрузка зон…";
        try
        {
            var list = await _warehouseService.GetZonesAsync(warehouseId);
            Zones.Clear();
            foreach (var z in list) Zones.Add(z);
            StatusMessage = Zones.Count > 0 ? string.Empty : "Зоны не найдены.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки зон: {ex.Message}";
        }
        finally
        {
            IsLoadingZones = false;
        }
    }

    // ── Zone → Shelves ─────────────────────────────────────────────────
    partial void OnSelectedZoneChanged(ZoneDto? value)
    {
        Shelves.Clear();
        Boxes.Clear();
        SelectedShelf = null;
        SelectedBox   = null;

        if (value is null) return;
        _ = LoadShelvesAsync(value.Id);
    }

    private async System.Threading.Tasks.Task LoadShelvesAsync(string zoneId)
    {
        IsLoadingShelves = true;
        StatusMessage = "Загрузка полок…";
        try
        {
            var list = await _warehouseService.GetShelvesAsync(zoneId);
            Shelves.Clear();
            foreach (var s in list) Shelves.Add(s);
            StatusMessage = Shelves.Count > 0 ? string.Empty : "Полки не найдены.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки полок: {ex.Message}";
        }
        finally
        {
            IsLoadingShelves = false;
        }
    }

    // ── Shelf → Boxes ──────────────────────────────────────────────────
    partial void OnSelectedShelfChanged(ShelfDto? value)
    {
        Boxes.Clear();
        SelectedBox = null;

        if (value is null) return;
        _ = LoadBoxesAsync(value.Id);
    }

    partial void OnSelectedBoxChanged(BoxDto? value)
    {
        var content = value?.LocationPath is { Length: > 0 } lp ? lp : value?.Id;
        QrPreview = string.IsNullOrWhiteSpace(content) ? null : RenderQrPreview(content, 256, 256);
    }

    private async System.Threading.Tasks.Task LoadBoxesAsync(string shelfId)
    {
        IsLoadingBoxes = true;
        StatusMessage = "Загрузка ячеек…";
        try
        {
            var list = await _warehouseService.GetBoxesAsync(shelfId);
            Boxes.Clear();
            foreach (var b in list) Boxes.Add(b);
            StatusMessage = Boxes.Count > 0 ? string.Empty : "Ячейки не найдены.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки ячеек: {ex.Message}";
        }
        finally
        {
            IsLoadingBoxes = false;
        }
    }

    // ── Print ──────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private void Print()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StatusMessage = "Печать поддерживается только на Windows.";
            return;
        }
        if (SelectedPrinter is null || SelectedBox is null) return;
        IsPrinting = true;
        StatusMessage = string.Empty;
        try
        {
            float dpiX, dpiY;
            using (var probe = new PrintDocument())
            {
                probe.PrinterSettings.PrinterName = SelectedPrinter;
                var res = probe.PrinterSettings.DefaultPageSettings.PrinterResolution;
                dpiX = res.X > 10 ? res.X : 203f;
                dpiY = res.Y > 10 ? res.Y : 203f;
            }

            int bmpW = (int)Math.Round(LabelWidth  * dpiX / 25.4);
            int bmpH = (int)Math.Round(LabelHeight * dpiY / 25.4);

            using var labelBmp = RenderPrintBitmap(SelectedBox, bmpW, bmpH, dpiX, dpiY);

            int wH = (int)Math.Round(LabelWidth  / 25.4 * 100);
            int hH = (int)Math.Round(LabelHeight / 25.4 * 100);
            var paperSize = new PaperSize("BoxLabel", wH, hH) { RawKind = 256 };

            for (int copy = 0; copy < Copies; copy++)
            {
                using var doc = new PrintDocument();
                doc.DocumentName = $"Box {SelectedBox.Code}";
                doc.PrinterSettings.PrinterName = SelectedPrinter;
                doc.DefaultPageSettings.PaperSize = paperSize;
                doc.DefaultPageSettings.Margins   = new Margins(0, 0, 0, 0);

                var bmp  = labelBmp;
                var _dpiX = dpiX;
                var _dpiY = dpiY;
                doc.PrintPage += (_, e) =>
                {
                    e.Graphics!.PageUnit = GraphicsUnit.Pixel;
                    float hmX = (float)(e.PageSettings.HardMarginX / 100.0 * _dpiX);
                    float hmY = (float)(e.PageSettings.HardMarginY / 100.0 * _dpiY);
                    e.Graphics.TranslateTransform(-hmX, -hmY);
                    e.Graphics.ResetClip();
                    e.Graphics.DrawImage(bmp, 0, 0, bmpW, bmpH);
                    e.HasMorePages = false;
                };
                doc.Print();
            }

            StatusMessage = $"Готово — {Copies} этикет(ок) отправлено на {SelectedPrinter}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private bool CanPrint() =>
        SelectedPrinter is not null &&
        SelectedBox     is not null &&
        !IsPrinting;

    partial void OnIsPrintingChanged(bool value) => PrintCommand.NotifyCanExecuteChanged();

    // ── Copies controls ────────────────────────────────────────────────
    [RelayCommand] private void IncreaseCopies() => Copies = Math.Min(999, Copies + 1);
    [RelayCommand] private void DecreaseCopies() => Copies = Math.Max(1,   Copies - 1);

    // ── Rendering helpers ──────────────────────────────────────────────

    private static AvaloniaBitmap? RenderQrPreview(string content, int w, int h)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format  = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions { Width = w, Height = h, Margin = 1, PureBarcode = true }
            };
            var pd = writer.Write(content);

            var wb = new WriteableBitmap(
                new PixelSize(pd.Width, pd.Height),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var fb = wb.Lock())
                Marshal.Copy(pd.Pixels, 0, fb.Address, pd.Pixels.Length);

            return wb;
        }
        catch { return null; }
    }

    private static System.Drawing.Bitmap RenderPrintBitmap(BoxDto box, int w, int h, float dpiX, float dpiY)
    {
        var bmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        bmp.SetResolution(dpiX, dpiY);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        int qrH = (int)(h * 0.75);
        var writer = new BarcodeWriterPixelData
        {
            Format  = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = w, Height = qrH, Margin = 1, PureBarcode = true }
        };
        var qrContent = box.LocationPath is { Length: > 0 } lp ? lp : box.Id;
        var pd = writer.Write(qrContent);
        using (var qrBmp = PixelDataToSDBitmap(pd))
            g.DrawImage(qrBmp, 0, 0, w, qrH);

        int textY = qrH + 4;
        int textH = h - qrH - 4;

        float pathFontSize = Math.Max(5f, dpiX / 25.4f * 2.8f);
        using var pathFont = new Font(FontFamily.GenericMonospace, pathFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush    = new SolidBrush(Color.Black);

        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.DrawString(qrContent, pathFont, brush, new RectangleF(0, textY, w, textH), sf);

        return bmp;
    }

    private static System.Drawing.Bitmap PixelDataToSDBitmap(PixelData pd)
    {
        var bmp = new System.Drawing.Bitmap(pd.Width, pd.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, pd.Width, pd.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        System.Runtime.InteropServices.Marshal.Copy(pd.Pixels, 0, bmpData.Scan0, pd.Pixels.Length);
        bmp.UnlockBits(bmpData);
        return bmp;
    }
}
