using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using Microsoft.Win32;

namespace LabelStudio.ViewModels;

/// <summary>
/// Shared singleton-style store so both the Profiles page and NewProject can share the same list.
/// </summary>
public static class PrinterProfileStore
{
    public static ObservableCollection<PrinterProfile> Profiles { get; } = [];

    static PrinterProfileStore()
    {
        // Default sample profiles — replaced by real data once user saves
        Profiles.Add(new PrinterProfile("Zebra 300 dpi Thermal", "Zebra ZT230", "300 dpi", "Thermal Transfer", true));
        Profiles.Add(new PrinterProfile("Office Color Inkjet", "HP OfficeJet Pro", "600 dpi", "Paper", false));
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Registry.LocalMachine?.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers");

            foreach (var name in key?.GetSubKeyNames() ?? [])
                SystemPrinters.Add(name);
        }

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
