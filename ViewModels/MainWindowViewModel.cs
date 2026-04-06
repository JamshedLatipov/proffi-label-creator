using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LabelStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentContent = new TemplatesViewModel();

    [RelayCommand]
    private void NavigateToTemplates()
    {
        CurrentContent = new TemplatesViewModel();
    }

    [RelayCommand]
    private void NavigateToPrintPreview()
    {
        CurrentContent = new PrintPreviewViewModel();
    }

    [RelayCommand]
    private void NavigateToEditor()
    {
        CurrentContent = new EditorViewModel();
    }
}
