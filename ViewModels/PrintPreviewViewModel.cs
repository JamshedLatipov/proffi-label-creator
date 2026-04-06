using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace LabelStudio.ViewModels;

public partial class PrintPreviewViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _printers = [];

    [ObservableProperty]
    private string? _selectedPrinter;

    public PrintPreviewViewModel()
    {
        LoadPrinters();
    }

    private void LoadPrinters()
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers");

        var names = key?.GetSubKeyNames() ?? [];
        Printers = new ObservableCollection<string>(names);
        SelectedPrinter = Printers.Count > 0 ? Printers[0] : null;
    }
}
