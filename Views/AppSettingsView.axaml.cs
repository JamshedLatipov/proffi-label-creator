using Avalonia.Controls;
using LabelStudio.ViewModels;

namespace LabelStudio.Views;

public partial class AppSettingsView : UserControl
{
    public AppSettingsView()
    {
        InitializeComponent();

        DataContextChanged += async (_, _) =>
        {
            if (DataContext is AppSettingsViewModel vm)
                await vm.LoadAsync();
        };
    }
}
