using System.Collections.ObjectModel;

namespace Editor.ViewModels;

public class ProjectListViewModel : ViewModelBase
{
    public ObservableCollection<Project> Projects { get; } = new()
    {
        new Project { Name = "Project 1" },
        new Project { Name = "Project 2" },
    };
}