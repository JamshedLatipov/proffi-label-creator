using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
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
    private ViewModelBase _currentContent = new TemplatesViewModel();

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

    [RelayCommand]
    private void NavigateToTemplates()
        => CurrentContent = new TemplatesViewModel();

    [RelayCommand]
    private void NavigateToProjects()
        => CurrentContent = new ProjectsViewModel(vm => CurrentContent = vm);

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
        var vm = new EditorViewModel();
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
        });

    [RelayCommand]
    private void NavigateToPrinterProfiles()
        => CurrentContent = new PrinterProfilesViewModel();

    /// <summary>Saves the current project if the editor is open.</summary>
    [RelayCommand]
    private void SaveCurrent()
    {
        if (CurrentContent is EditorViewModel editor)
            editor.SaveCommand.Execute(null);
    }
}
