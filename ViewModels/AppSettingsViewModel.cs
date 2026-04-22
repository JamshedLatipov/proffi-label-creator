using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class AppSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService  _settingsService;
    private readonly IWarehouseService _warehouseService;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    [ObservableProperty]
    private WarehouseDto? _selectedWarehouse;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public event EventHandler? BackRequested;
    public event EventHandler? Saved;

    public AppSettingsViewModel(ISettingsService settingsService, IWarehouseService warehouseService)
    {
        _settingsService  = settingsService;
        _warehouseService = warehouseService;
    }

    public async Task LoadAsync()
    {
        IsLoading     = true;
        StatusMessage = string.Empty;
        HasError      = false;
        try
        {
            var list = await _warehouseService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in list)
                Warehouses.Add(w);

            // Restore previously saved selection
            if (!string.IsNullOrEmpty(_settingsService.DefaultWarehouseId))
            {
                foreach (var w in Warehouses)
                {
                    if (w.Id == _settingsService.DefaultWarehouseId)
                    {
                        SelectedWarehouse = w;
                        break;
                    }
                }
            }

            if (Warehouses.Count == 0)
            {
                StatusMessage = "No warehouses found. Check your connection settings.";
                HasError = true;
            }
        }
        catch
        {
            StatusMessage = "Failed to load warehouses.";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedWarehouse is null)
        {
            StatusMessage = "Please select a warehouse.";
            HasError = true;
            return;
        }

        _settingsService.DefaultWarehouseId   = SelectedWarehouse.Id;
        _settingsService.DefaultWarehouseName = SelectedWarehouse.Name;
        _settingsService.Save();

        StatusMessage = "Saved!";
        HasError = false;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
