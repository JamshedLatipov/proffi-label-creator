using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class TemplatesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<TemplateItemViewModel> _templates = new();

    public TemplatesViewModel() : this(_ => { }, new SettingsService()) { }

    public TemplatesViewModel(Action<ViewModelBase> navigate, ISettingsService settingsService)
    {
        Templates.Add(new TemplateItemViewModel(
            "Shipping Standard XL", "Logistics", "100x150 mm",
            "Штрихкод + QR трекинга, поля получателя и адреса.",
            navigate, settingsService,
            BuildShippingLabel()));

        Templates.Add(new TemplateItemViewModel(
            "Inventory Asset Tag", "Inventory", "58x40 mm",
            "QR-код актива, штрихкод и поля наименования.",
            navigate, settingsService,
            BuildAssetTag()));

        Templates.Add(new TemplateItemViewModel(
            "Component Warning", "Safety", "40x80 mm",
            "Предупреждающая этикетка с рамкой и штрихкодом.",
            navigate, settingsService,
            BuildWarningLabel()));

        Templates.Add(new TemplateItemViewModel(
            "Product Retail Label", "Retail", "110x35 mm",
            "Торговая этикетка: штрихкод EAN, цена, наименование.",
            navigate, settingsService,
            BuildRetailLabel()));
    }

    // ── Template factories ────────────────────────────────────────────────

    private static LabelProject BuildShippingLabel() => new()
    {
        Name        = "Shipping Standard XL",
        Description = "Логистическая этикетка отправления",
        LabelWidth  = 100,
        LabelHeight = 150,
        IsPortrait  = true,
        Dpi         = "300 dpi",
        Material    = "Thermal Transfer",
        IsMonochrome = true,
        Elements =
        [
            // Outer border
            new() { Kind = ElementKind.Rectangle, X = 2,  Y = 2,  Width = 96,  Height = 146 },

            // Header
            new() { Kind = ElementKind.Text, X = 5, Y = 5, Width = 90, Height = 8,
                    Content = "SHIPPING LABEL", FontFamily = "Manrope", FontSize = 14, Bold = true },

            // Separator line
            new() { Kind = ElementKind.Rectangle, X = 5, Y = 15, Width = 90, Height = 0.5 },

            // Recipient dynamic fields
            new() { Kind = ElementKind.Text,         X = 5,  Y = 18, Width = 30, Height = 5,
                    Content = "Получатель:", FontSize = 7, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 36, Y = 18, Width = 59, Height = 5,
                    Content = "recipient_name", FontSize = 8 },
            new() { Kind = ElementKind.DynamicField, X = 5,  Y = 25, Width = 90, Height = 5,
                    Content = "recipient_address", FontSize = 8 },
            new() { Kind = ElementKind.DynamicField, X = 5,  Y = 31, Width = 90, Height = 5,
                    Content = "recipient_city", FontSize = 8 },

            // Separator
            new() { Kind = ElementKind.Rectangle, X = 5, Y = 38, Width = 90, Height = 0.5 },

            // Barcode (tracking)
            new() { Kind = ElementKind.Barcode, X = 10, Y = 42, Width = 80, Height = 22,
                    BarcodeValue = "123456789012" },

            // Tracking number text
            new() { Kind = ElementKind.Text, X = 5, Y = 65, Width = 90, Height = 5,
                    Content = "Трек: 123456789012", FontFamily = "Consolas, Monospace", FontSize = 7 },

            // Separator
            new() { Kind = ElementKind.Rectangle, X = 5, Y = 72, Width = 90, Height = 0.5 },

            // QR code
            new() { Kind = ElementKind.QrCode, X = 30, Y = 76, Width = 40, Height = 40,
                    Content = "https://track.example.com/123456789012" },

            // QR label
            new() { Kind = ElementKind.Text, X = 5, Y = 118, Width = 90, Height = 5,
                    Content = "Отсканируйте для отслеживания", FontSize = 7 },

            // Footer separator
            new() { Kind = ElementKind.Rectangle, X = 5, Y = 125, Width = 90, Height = 0.5 },

            // Footer weight / dims
            new() { Kind = ElementKind.Text,         X = 5,  Y = 128, Width = 30, Height = 5,
                    Content = "Вес:", FontSize = 7, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 36, Y = 128, Width = 30, Height = 5,
                    Content = "weight_kg", FontSize = 7 },
            new() { Kind = ElementKind.Text,         X = 5,  Y = 135, Width = 30, Height = 5,
                    Content = "Р-р:", FontSize = 7, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 36, Y = 135, Width = 55, Height = 5,
                    Content = "dimensions", FontSize = 7 },
        ]
    };

    private static LabelProject BuildAssetTag() => new()
    {
        Name        = "Inventory Asset Tag",
        Description = "Инвентарная метка актива",
        LabelWidth  = 58,
        LabelHeight = 40,
        IsPortrait  = false,
        Dpi         = "300 dpi",
        Material    = "Thermal Transfer",
        IsMonochrome = true,
        Elements =
        [
            // Border
            new() { Kind = ElementKind.Rectangle, X = 1, Y = 1, Width = 56, Height = 38 },

            // QR code
            new() { Kind = ElementKind.QrCode, X = 3, Y = 3, Width = 28, Height = 28,
                    Content = "ASSET-000001" },

            // Header
            new() { Kind = ElementKind.Text, X = 33, Y = 3, Width = 23, Height = 6,
                    Content = "АКТИВ", FontFamily = "Manrope", FontSize = 9, Bold = true },

            // Asset name
            new() { Kind = ElementKind.DynamicField, X = 33, Y = 10, Width = 23, Height = 5,
                    Content = "asset_name", FontSize = 7 },

            // Department
            new() { Kind = ElementKind.Text,         X = 33, Y = 16, Width = 10, Height = 4,
                    Content = "Отдел:", FontSize = 6, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 43, Y = 16, Width = 14, Height = 4,
                    Content = "department", FontSize = 6 },

            // Barcode at bottom
            new() { Kind = ElementKind.Barcode, X = 3, Y = 33, Width = 52, Height = 5,
                    BarcodeValue = "AST000001" },
        ]
    };

    private static LabelProject BuildWarningLabel() => new()
    {
        Name        = "Component Warning",
        Description = "Предупреждающая этикетка",
        LabelWidth  = 40,
        LabelHeight = 80,
        IsPortrait  = true,
        Dpi         = "300 dpi",
        Material    = "Thermal Transfer",
        IsMonochrome = true,
        Elements =
        [
            // Outer border
            new() { Kind = ElementKind.Rectangle, X = 1, Y = 1, Width = 38, Height = 78 },

            // Warning header background (thick border inner)
            new() { Kind = ElementKind.Rectangle, X = 2, Y = 2, Width = 36, Height = 14 },

            // Warning title
            new() { Kind = ElementKind.Text, X = 3, Y = 4, Width = 34, Height = 10,
                    Content = "⚠ ВНИМАНИЕ", FontFamily = "Manrope", FontSize = 10, Bold = true,
                    TextBackground = "#FFFF00" },

            // Separator
            new() { Kind = ElementKind.Rectangle, X = 3, Y = 17, Width = 34, Height = 0.5 },

            // Static warning text
            new() { Kind = ElementKind.Text, X = 3, Y = 20, Width = 34, Height = 8,
                    Content = "ОСТОРОЖНО!", FontSize = 10, Bold = true },
            new() { Kind = ElementKind.Text, X = 3, Y = 30, Width = 34, Height = 5,
                    Content = "Хрупкое содержимое.", FontSize = 7 },
            new() { Kind = ElementKind.Text, X = 3, Y = 36, Width = 34, Height = 5,
                    Content = "Не бросать, не переворачивать.", FontSize = 7 },

            // Dynamic hazard field
            new() { Kind = ElementKind.Text,         X = 3, Y = 44, Width = 14, Height = 4,
                    Content = "Код:", FontSize = 6, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 18, Y = 44, Width = 21, Height = 4,
                    Content = "hazard_code", FontSize = 6 },

            // Separator
            new() { Kind = ElementKind.Rectangle, X = 3, Y = 50, Width = 34, Height = 0.5 },

            // Barcode
            new() { Kind = ElementKind.Barcode, X = 4, Y = 53, Width = 32, Height = 18,
                    BarcodeValue = "WRN000001" },
        ]
    };

    private static LabelProject BuildRetailLabel() => new()
    {
        Name        = "Product Retail Label",
        Description = "Торговая этикетка продукта",
        LabelWidth  = 110,
        LabelHeight = 35,
        IsPortrait  = false,
        Dpi         = "300 dpi",
        Material    = "Paper",
        IsMonochrome = false,
        Elements =
        [
            // Border
            new() { Kind = ElementKind.Rectangle, X = 1, Y = 1, Width = 108, Height = 33 },

            // Product name
            new() { Kind = ElementKind.DynamicField, X = 4, Y = 3, Width = 65, Height = 7,
                    Content = "product_name", FontFamily = "Manrope", FontSize = 11, Bold = true },

            // Article
            new() { Kind = ElementKind.Text,         X = 4, Y = 11, Width = 12, Height = 5,
                    Content = "Арт:", FontSize = 7, Bold = true },
            new() { Kind = ElementKind.DynamicField, X = 17, Y = 11, Width = 30, Height = 5,
                    Content = "article", FontFamily = "Consolas, Monospace", FontSize = 7 },

            // Price
            new() { Kind = ElementKind.DynamicField, X = 4, Y = 18, Width = 30, Height = 9,
                    Content = "price", FontFamily = "Manrope", FontSize = 14, Bold = true },
            new() { Kind = ElementKind.Text, X = 35, Y = 22, Width = 10, Height = 5,
                    Content = "руб.", FontSize = 8 },

            // Vertical separator
            new() { Kind = ElementKind.Rectangle, X = 72, Y = 3, Width = 0.5, Height = 29 },

            // Barcode
            new() { Kind = ElementKind.Barcode, X = 75, Y = 3, Width = 32, Height = 22,
                    BarcodeValue = "4607035722714" },

            // EAN digits
            new() { Kind = ElementKind.DynamicField, X = 75, Y = 26, Width = 32, Height = 4,
                    Content = "barcode", FontFamily = "Consolas, Monospace", FontSize = 6 },
        ]
    };
}

public partial class TemplateItemViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;
    private readonly ISettingsService _settingsService;
    private readonly LabelProject _project;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _category;
    [ObservableProperty] private string _size;
    [ObservableProperty] private string _description;

    public TemplateItemViewModel(string title, string category, string size, string description,
        Action<ViewModelBase> navigate, ISettingsService settingsService, LabelProject project)
    {
        Title            = title;
        Category         = category;
        Size             = size;
        Description      = description;
        _navigate        = navigate;
        _settingsService = settingsService;
        _project         = project;
        Preview          = new EditorViewModel(settingsService, project);
    }

    public EditorViewModel Preview { get; }

    [RelayCommand]
    private void Open() => _navigate(new EditorViewModel(_settingsService, _project));
}
