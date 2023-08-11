using System.Windows.Input;
using ReactiveUI;

namespace Editor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ViewModelBase[] _pages =
    {
        new HomeViewModel(),
        new CreateProjectViewModel(),
    };

    private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        _currentPage = _pages[0];

        NavigateHomeCommand = ReactiveCommand.Create(NavigateHome);
        NavigateNewProjectCommand = ReactiveCommand.Create(NavigateNewProject);
    }

    /// <summary>
    /// Gets the current page. The property is read-only
    /// </summary>
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateNewProjectCommand { get; }

    private void NavigateHome()
    {
        CurrentPage = _pages[0];
    }

    private void NavigateNewProject()
    {
        CurrentPage = _pages[1];
    }
}