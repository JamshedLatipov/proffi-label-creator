using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class NewProjectViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;
    private readonly ISettingsService _settingsService;

    // ── 1. Project Details ──────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    // ── 2. Label Format ─────────────────────────────────────────────────
    private static readonly (string Label, double W, double H)[] PresetData =
    [
        ("58×40",   58,  40),
        ("100×150", 100, 150),
        ("110×35",  110, 35),
        ("40×80",   40,  80),
        ("Custom",  0,   0),
    ];

    public ObservableCollection<string> Presets { get; } =
        ["58×40", "100×150", "110×35", "40×80", "Custom"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomSize))]
    private string _selectedPreset = "58×40";

    public bool IsCustomSize => SelectedPreset == "Custom";

    partial void OnSelectedPresetChanged(string value)
    {
        foreach (var (label, w, h) in PresetData)
        {
            if (label != value) continue;
            if (w == 0) break;
            LabelWidth = w;
            LabelHeight = h;
            break;
        }
    }

    [ObservableProperty]
    private double _labelWidth = 58;

    [ObservableProperty]
    private double _labelHeight = 40;

    [ObservableProperty]
    private bool _isPortrait = true;

    partial void OnIsPortraitChanged(bool value)
    {
        (LabelWidth, LabelHeight) = (LabelHeight, LabelWidth);
    }

    // ── 3. Printer Profile ──────────────────────────────────────────────
    public ObservableCollection<PrinterProfile> PrinterProfiles => PrinterProfileStore.Profiles;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private PrinterProfile? _selectedPrinterProfile;

    // ─────────────────────────────────────────────────────────────────────
    public NewProjectViewModel(Action<ViewModelBase> navigate, ISettingsService settingsService)
    {
        _navigate         = navigate;
        _settingsService  = settingsService;
        LabelWidth  = 58;
        LabelHeight = 40;
        SelectedPrinterProfile = PrinterProfiles.Count > 0 ? PrinterProfiles[0] : null;
    }

    // ── Commands ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectPreset(string preset) => SelectedPreset = preset;

    [RelayCommand]
    private void SetPortrait()  { if (!IsPortrait) IsPortrait = true; }

    [RelayCommand]
    private void SetLandscape() { if (IsPortrait)  IsPortrait = false; }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private void CreateProject()
    {
        var p = SelectedPrinterProfile!;
        var settings = new ProjectSettings(
            ProjectName:         ProjectName.Trim(),
            Description:         Description.Trim(),
            LabelWidth:          LabelWidth,
            LabelHeight:         LabelHeight,
            IsPortrait:          IsPortrait,
            PrinterProfileName:  p.Name,
            PrinterName:         p.PrinterName,
            Dpi:                 p.Dpi,
            Material:            p.Material,
            IsMonochrome:        p.IsMonochrome
        );

        _navigate(new EditorViewModel(_settingsService, settings));
    }

    private bool CanCreateProject() =>
        !string.IsNullOrWhiteSpace(ProjectName) && SelectedPrinterProfile is not null;

    [RelayCommand]
    private void Cancel() => _navigate(new TemplatesViewModel());
}
