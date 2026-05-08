using System;
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
        Templates.Add(new TemplateItemViewModel("Shipping Standard XL",  "Logistics", "100x150 mm", "Thermal transfer, high durability.",      navigate, settingsService));
        Templates.Add(new TemplateItemViewModel("Inventory Asset Tag",    "Inventory", "58x40 mm",   "Serialized QR code identification.",     navigate, settingsService));
        Templates.Add(new TemplateItemViewModel("Component Warning",      "Safety",    "40x80 mm",   "ANSI standard cautionary layout.",       navigate, settingsService));
        Templates.Add(new TemplateItemViewModel("Product Retail Label",   "Retail",    "110x35 mm",  "Multi-layer ink application.",           navigate, settingsService));
    }
}

public partial class TemplateItemViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _category;
    [ObservableProperty] private string _size;
    [ObservableProperty] private string _description;

    public TemplateItemViewModel(string title, string category, string size, string description,
        Action<ViewModelBase> navigate, ISettingsService settingsService)
    {
        Title           = title;
        Category        = category;
        Size            = size;
        Description     = description;
        _navigate       = navigate;
        _settingsService = settingsService;
    }

    [RelayCommand]
    private void Open()
    {
        var (w, h) = ParseSize(Size);
        var settings = new ProjectSettings(
            ProjectName:        Title,
            Description:        Description,
            LabelWidth:         w,
            LabelHeight:        h,
            IsPortrait:         h > w,
            PrinterProfileName: string.Empty,
            PrinterName:        string.Empty,
            Dpi:                "300 dpi",
            Material:           "Thermal Transfer",
            IsMonochrome:       true
        );
        _navigate(new EditorViewModel(_settingsService, settings));
    }

    /// <summary>Parses "100x150 mm" → (100, 150). Falls back to 100×50 on parse failure.</summary>
    private static (double w, double h) ParseSize(string size)
    {
        var parts = size.Replace(" mm", "").Split('x');
        if (parts.Length == 2
            && double.TryParse(parts[0], out var w)
            && double.TryParse(parts[1], out var h))
            return (w, h);
        return (100, 50);
    }
}
