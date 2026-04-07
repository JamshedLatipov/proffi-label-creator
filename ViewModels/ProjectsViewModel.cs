using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelStudio.Models;
using LabelStudio.Services;

namespace LabelStudio.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;

    public ObservableCollection<LabelProjectViewModel> Projects { get; } = [];

    public bool HasNoProjects => Projects.Count == 0;

    public ProjectsViewModel(Action<ViewModelBase> navigate)
    {
        _navigate = navigate;
        Projects.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoProjects));
        Reload();
    }

    private void Reload()
    {
        Projects.Clear();
        foreach (var p in ProjectService.LoadAll())
            Projects.Add(new LabelProjectViewModel(p, OpenProject, DeleteProject));
        OnPropertyChanged(nameof(HasNoProjects));
    }

    private void OpenProject(LabelProjectViewModel item)
    {
        var editor = new EditorViewModel(item.Project);
        editor.Navigate = _navigate;
        _navigate(editor);
    }

    private void DeleteProject(LabelProjectViewModel item)
    {
        ProjectService.Delete(item.Project.Id);
        Projects.Remove(item);
    }

    [RelayCommand]
    private void NewProject()
        => _navigate(new NewProjectViewModel(_navigate));
}

public class LabelProjectViewModel
{
    public LabelProject Project { get; }

    public string Name          => Project.Name;
    public string Description   => Project.Description;
    public string SizeLabel     => $"{Project.LabelWidth:0.#} × {Project.LabelHeight:0.#} mm";
    public string ProfileLabel  => string.IsNullOrEmpty(Project.PrinterProfileName)
                                   ? "—" : Project.PrinterProfileName;
    public string ModifiedLabel => Project.ModifiedAt.ToLocalTime().ToString("MMM d, yyyy");
    public string InitialLetter => string.IsNullOrEmpty(Project.Name)
                                   ? "?" : Project.Name[0].ToString().ToUpper();

    public IRelayCommand OpenCommand   { get; }
    public IRelayCommand DeleteCommand { get; }

    public LabelProjectViewModel(
        LabelProject project,
        Action<LabelProjectViewModel> onOpen,
        Action<LabelProjectViewModel> onDelete)
    {
        Project       = project;
        OpenCommand   = new RelayCommand(() => onOpen(this));
        DeleteCommand = new RelayCommand(() => onDelete(this));
    }
}
