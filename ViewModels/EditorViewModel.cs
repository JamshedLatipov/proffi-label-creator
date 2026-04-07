using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly LabelProject _project;

    // Navigation callback (set by MainWindowViewModel when not navigating standalone)
    public Action<ViewModelBase>? Navigate { get; set; }

    // ── Project meta ───────────────────────────────────────────────────
    [ObservableProperty] private string _projectName = "Untitled";
    [ObservableProperty] private double _labelWidth  = 100;
    [ObservableProperty] private double _labelHeight = 150;
    [ObservableProperty] private bool   _isPortrait  = true;
    [ObservableProperty] private string _printerProfileName = string.Empty;
    [ObservableProperty] private bool   _isDirty;

    // ── Canvas scale: mm → pixels (96 dpi, 1 mm ≈ 3.7795 px) ─────────
    private const double MmToPx = 3.7795;
    public double CanvasWidth  => LabelWidth  * MmToPx;
    public double CanvasHeight => LabelHeight * MmToPx;

    // ── Elements ───────────────────────────────────────────────────────
    public ObservableCollection<ElementViewModel> Elements { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(BringForwardCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendBackwardCommand))]
    private ElementViewModel? _selectedElement;

    public bool HasSelection => SelectedElement is not null;

    // ── Constructors ────────────────────────────────────────────────────

    public EditorViewModel()
    {
        _project = new LabelProject { Name = "Untitled" };
    }

    public EditorViewModel(ProjectSettings settings)
    {
        _project            = LabelProject.FromSettings(settings);
        _projectName        = _project.Name;
        _labelWidth         = _project.LabelWidth;
        _labelHeight        = _project.LabelHeight;
        _isPortrait         = _project.IsPortrait;
        _printerProfileName = _project.PrinterProfileName;
        _isDirty            = true;
        LoadElements();
    }

    public EditorViewModel(LabelProject project)
    {
        _project            = project;
        _projectName        = project.Name;
        _labelWidth         = project.LabelWidth;
        _labelHeight        = project.LabelHeight;
        _isPortrait         = project.IsPortrait;
        _printerProfileName = project.PrinterProfileName;
        LoadElements();
    }

    private void LoadElements()
    {
        foreach (var el in _project.Elements)
            Elements.Add(new ElementViewModel(el, this));
    }

    // ── Change tracking ──────────────────────────────────────────────────

    partial void OnProjectNameChanged(string value)
    {
        _project.Name = value;
        IsDirty = true;
    }

    partial void OnLabelWidthChanged(double value)
    {
        _project.LabelWidth = value;
        IsDirty = true;
        OnPropertyChanged(nameof(CanvasWidth));
    }

    partial void OnLabelHeightChanged(double value)
    {
        _project.LabelHeight = value;
        IsDirty = true;
        OnPropertyChanged(nameof(CanvasHeight));
    }

    partial void OnIsPortraitChanged(bool value)
    {
        _project.IsPortrait = value;
        IsDirty = true;
    }

    public void MarkDirty() => IsDirty = true;

    // ── Element commands ─────────────────────────────────────────────────

    public void SelectElement(ElementViewModel? el)
    {
        if (SelectedElement is not null) SelectedElement.IsSelected = false;
        SelectedElement = el;
        if (SelectedElement is not null) SelectedElement.IsSelected = true;
    }

    [RelayCommand]
    private void AddText()       => AddElement(ElementKind.Text,         30, 8);

    [RelayCommand]
    private void AddDynamicField() => AddElement(ElementKind.DynamicField, 40, 8);

    [RelayCommand]
    private void AddBarcode()    => AddElement(ElementKind.Barcode,      60, 20);

    [RelayCommand]
    private void AddQrCode()     => AddElement(ElementKind.QrCode,       20, 20);

    [RelayCommand]
    private void AddRectangle()  => AddElement(ElementKind.Rectangle,    30, 15);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (SelectedElement is null) return;
        _project.Elements.Remove(SelectedElement.Model);
        Elements.Remove(SelectedElement);
        SelectElement(null);
        IsDirty = true;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void BringForward()
    {
        if (SelectedElement is null) return;
        var i = Elements.IndexOf(SelectedElement);
        if (i < Elements.Count - 1) Elements.Move(i, i + 1);
        IsDirty = true;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SendBackward()
    {
        if (SelectedElement is null) return;
        var i = Elements.IndexOf(SelectedElement);
        if (i > 0) Elements.Move(i, i - 1);
        IsDirty = true;
    }

    private void AddElement(ElementKind kind, double wMm, double hMm)
    {
        var model = new LabelElement
        {
            Kind    = kind,
            X       = 5, Y = 5,
            Width   = wMm,
            Height  = hMm,
            Content = kind == ElementKind.DynamicField ? "{Field}" : kind == ElementKind.Text ? "Text" : string.Empty,
            BarcodeValue = kind == ElementKind.Barcode ? "0000000000000" : string.Empty,
        };
        _project.Elements.Add(model);
        var vm = new ElementViewModel(model, this);
        Elements.Add(vm);
        SelectElement(vm);
        IsDirty = true;
    }

    // ── Save & Print ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        _project.Name       = ProjectName;
        _project.ModifiedAt = DateTime.UtcNow;
        ProjectService.Save(_project);
        IsDirty = false;
    }

    [RelayCommand]
    private void NavigateToPrintPreview()
    {
        Save();
        Navigate?.Invoke(new PrintPreviewViewModel(this));
    }
}
