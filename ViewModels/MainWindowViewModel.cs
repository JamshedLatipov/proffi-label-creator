using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService  _settingsService;
    private readonly IWarehouseService _warehouseService;

    public event EventHandler? LogoutRequested;

    public MainWindowViewModel(ISettingsService settingsService, IWarehouseService warehouseService)
    {
        _settingsService  = settingsService;
        _warehouseService = warehouseService;
        _currentContent   = new TemplatesViewModel(vm => CurrentContent = vm, settingsService);
    }

    // ── Auth state ──────────────────────────────────────────────────────────
    /// <summary>True once the user has successfully signed in.</summary>
    [ObservableProperty]
    private bool _isAuthenticated;

    /// <summary>
    /// ViewModel shown in the full-screen auth shell
    /// (LoginViewModel or AuthSettingsViewModel).
    /// Null when the main app layout is visible.
    /// </summary>
    [ObservableProperty]
    private ViewModelBase? _authContent;

    // ── Main app state ──────────────────────────────────────────────────────
    [ObservableProperty]
    private ViewModelBase _currentContent = null!; // set in constructor

    partial void OnCurrentContentChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(CurrentProjectTitle));
        OnPropertyChanged(nameof(IsDirtyIndicator));
        NavigateToPrintPreviewCommand.NotifyCanExecuteChanged();
        SaveCurrentCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Shows the open project name in the toolbar, or the app name otherwise.</summary>
    public string CurrentProjectTitle =>
        CurrentContent is EditorViewModel e ? e.ProjectName : "Label Studio";

    /// <summary>Asterisk shown in toolbar when there are unsaved changes.</summary>
    public string IsDirtyIndicator =>
        CurrentContent is EditorViewModel { IsDirty: true } ? " *" : string.Empty;

    /// <summary>Name of the currently saved default warehouse (shown in toolbar).</summary>
    public string DefaultWarehouseName => _settingsService.DefaultWarehouseName;

    /// <summary>True when a default warehouse has been selected.</summary>
    public bool HasDefaultWarehouse => !string.IsNullOrWhiteSpace(_settingsService.DefaultWarehouseName);

    /// <summary>Logged-in user email shown in sidebar footer.</summary>
    public string LoggedInUser => _settingsService.UserEmail is { Length: > 0 } email ? email : "Connected";

    [RelayCommand]
    private void NavigateToTemplates()
        => CurrentContent = new TemplatesViewModel(vm => CurrentContent = vm, _settingsService);

    [RelayCommand]
    private void NavigateToProjects()
        => CurrentContent = new ProjectsViewModel(vm => CurrentContent = vm, _settingsService);

    [RelayCommand(CanExecute = nameof(IsEditorActive))]
    private void NavigateToPrintPreview()
    {
        if (CurrentContent is not EditorViewModel activeEditor) return;
        CurrentContent = new PrintPreviewViewModel(activeEditor, _settingsService);
    }

    private bool IsEditorActive() => CurrentContent is EditorViewModel;

    [RelayCommand]
    private void NavigateToEditor()
    {
        var vm = new EditorViewModel(_settingsService);
        vm.Navigate = nav => CurrentContent = nav;
        CurrentContent = vm;
    }

    [RelayCommand]
    private void NavigateToNewProject()
        => CurrentContent = new NewProjectViewModel(vm =>
        {
            if (vm is EditorViewModel editor)
                editor.Navigate = nav => CurrentContent = nav;
            CurrentContent = vm;
        }, _settingsService);

    [RelayCommand]
    private void NavigateToPrinterProfiles()
        => CurrentContent = new PrinterProfilesViewModel();

    [RelayCommand]
    private void NavigateToAppSettings()
    {
        var vm = new AppSettingsViewModel(_settingsService, _warehouseService);
        vm.BackRequested += (_, _) => CurrentContent = new TemplatesViewModel();
        vm.Saved += (_, _) =>
        {
            OnPropertyChanged(nameof(DefaultWarehouseName));
            OnPropertyChanged(nameof(HasDefaultWarehouse));
        };
        CurrentContent = vm;
    }

    /// <summary>Saves the current project if the editor is open.</summary>
    [RelayCommand]
    private void SaveCurrent()
    {
        if (CurrentContent is EditorViewModel editor)
            editor.SaveCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToZoneLabel()
        => CurrentContent = new ZoneLabelViewModel(_warehouseService);

    [RelayCommand]
    private void Logout()
        => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
