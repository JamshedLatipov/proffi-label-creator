using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class AuthSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _backendUrl = string.Empty;

    [ObservableProperty]
    private string _savedMessage = string.Empty;

    public event EventHandler? BackRequested;

    public AuthSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        BackendUrl = settingsService.BackendUrl;
    }

    [RelayCommand]
    private void Save()
    {
        _settingsService.BackendUrl = BackendUrl.Trim();
        _settingsService.Save();
        SavedMessage = "Settings saved";
    }

    [RelayCommand]
    private void Back()
        => BackRequested?.Invoke(this, EventArgs.Empty);
}
