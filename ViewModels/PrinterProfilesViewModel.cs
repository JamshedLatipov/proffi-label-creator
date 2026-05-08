using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

/// <summary>
/// Shared singleton-style store so both the Profiles page and NewProject can share the same list.
/// </summary>
public static class PrinterProfileStore
{
    private static readonly string ProfilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LabelStudio", "printer-profiles.json");

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static ObservableCollection<PrinterProfile> Profiles { get; } = [];

    static PrinterProfileStore()
    {
        var loaded = Load();
        if (loaded.Count > 0)
        {
            foreach (var p in loaded)
                Profiles.Add(p);
        }
        else
        {
            // Built-in defaults shown until user creates real profiles
            Profiles.Add(new PrinterProfile("Zebra 300 dpi Thermal", "Zebra ZT230", "300 dpi", "Thermal Transfer", true));
            Profiles.Add(new PrinterProfile("Office Color Inkjet", "HP OfficeJet Pro", "600 dpi", "Paper", false));
        }
    }

    public static void Save()
    {
        var dir = Path.GetDirectoryName(ProfilesPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ProfilesPath, JsonSerializer.Serialize(Profiles.ToList(), _opts));
    }

    private static List<PrinterProfile> Load()
    {
        if (!File.Exists(ProfilesPath)) return [];
        try
        {
            var text = File.ReadAllText(ProfilesPath);
            return JsonSerializer.Deserialize<List<PrinterProfile>>(text, _opts) ?? [];
        }
        catch { return []; }
    }
}

public partial class PrinterProfilesViewModel : ViewModelBase
{
    // ── Real printers from system ──────────────────────────────────────
    public ObservableCollection<string> SystemPrinters { get; } = [];

    public ObservableCollection<string> Materials { get; } =
        ["Thermal Transfer", "Direct Thermal", "Vinyl", "Paper"];

    public ObservableCollection<string> DpiOptions { get; } =
        ["203 dpi", "300 dpi", "600 dpi"];

    // ── Profiles list ──────────────────────────────────────────────────
    public ObservableCollection<PrinterProfile> Profiles => PrinterProfileStore.Profiles;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProfileCommand))]
    private PrinterProfile? _selectedProfile;

    // ── Form (create / edit) ───────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string? _formPrinterName;

    [ObservableProperty]
    private string _formDpi = "300 dpi";

    [ObservableProperty]
    private string _formMaterial = "Thermal Transfer";

    [ObservableProperty]
    private bool _formIsMonochrome = true;

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private bool _isEditing;

    private PrinterProfile? _editingTarget;

    // ─────────────────────────────────────────────────────────────────────
    public PrinterProfilesViewModel()
    {
        LoadSystemPrinters();
    }

    private void LoadSystemPrinters()
    {
        foreach (var name in PrinterService.GetSystemPrinters())
            SystemPrinters.Add(name);

        FormPrinterName = SystemPrinters.Count > 0 ? SystemPrinters[0] : null;
    }

    // ── Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void ShowAddForm()
    {
        _editingTarget = null;
        IsEditing = false;
        FormName = string.Empty;
        FormPrinterName = SystemPrinters.Count > 0 ? SystemPrinters[0] : null;
        FormDpi = "300 dpi";
        FormMaterial = "Thermal Transfer";
        FormIsMonochrome = true;
        IsFormVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditProfile()
    {
        if (SelectedProfile is null) return;
        _editingTarget = SelectedProfile;
        IsEditing = true;
        FormName        = SelectedProfile.Name;
        FormPrinterName = SelectedProfile.PrinterName;
        FormDpi         = SelectedProfile.Dpi;
        FormMaterial    = SelectedProfile.Material;
        FormIsMonochrome = SelectedProfile.IsMonochrome;
        IsFormVisible   = true;
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void DeleteProfile()
    {
        if (SelectedProfile is null) return;
        Profiles.Remove(SelectedProfile);
        SelectedProfile = null;
        PrinterProfileStore.Save();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveProfile()
    {
        var profile = new PrinterProfile(
            Name:        FormName.Trim(),
            PrinterName: FormPrinterName ?? string.Empty,
            Dpi:         FormDpi,
            Material:    FormMaterial,
            IsMonochrome: FormIsMonochrome
        );

        if (_editingTarget is not null)
        {
            var idx = Profiles.IndexOf(_editingTarget);
            if (idx >= 0) Profiles[idx] = profile;
        }
        else
        {
            Profiles.Add(profile);
        }

        PrinterProfileStore.Save();
        CancelForm();
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormVisible = false;
        _editingTarget = null;
    }

    private bool CanEditOrDelete() => SelectedProfile is not null;
    private bool CanSave() => !string.IsNullOrWhiteSpace(FormName);
}
