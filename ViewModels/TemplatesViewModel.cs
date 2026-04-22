using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelStudio.ViewModels;

public partial class TemplatesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<TemplateItemViewModel> _templates = new();

    public TemplatesViewModel()
    {
        Templates.Add(new TemplateItemViewModel("Shipping Standard XL", "Logistics", "100x150 mm", "Thermal transfer, high durability."));
        Templates.Add(new TemplateItemViewModel("Inventory Asset Tag", "Inventory", "58x40 mm", "Serialized QR code identification."));
        Templates.Add(new TemplateItemViewModel("Component Warning", "Safety", "40x80 mm", "ANSI standard cautionary layout."));
        Templates.Add(new TemplateItemViewModel("Product Retail Label", "Retail", "110x35 mm", "Multi-layer ink application."));
    }
}

public partial class TemplateItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _category;

    [ObservableProperty]
    private string _size;

    [ObservableProperty]
    private string _description;

    public TemplateItemViewModel(string title, string category, string size, string description)
    {
        Title = title;
        Category = category;
        Size = size;
        Description = description;
    }
}
