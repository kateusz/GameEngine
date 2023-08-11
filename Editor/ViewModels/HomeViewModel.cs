using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Editor.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ReactiveUI;

namespace Editor.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly Lazy<IFilesService> _filesServiceLazy = new(() =>
    {
        var filesService = App.Services?.GetRequiredService<IFilesService>();
        return filesService ?? throw new NullReferenceException("Missing File Service instance.");
    });

    private IFilesService FilesService => _filesServiceLazy.Value;

    public ProjectListViewModel ProjectList { get; }
    public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }

    public HomeViewModel()
    {
        ProjectList = new ProjectListViewModel();
        NewProjectCommand = ReactiveCommand.Create(CreateNewProject);
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenExistingProject);
    }

    private void CreateNewProject()
    {
    }

    private async Task OpenExistingProject()
    {
        var file = await FilesService.OpenFileAsync();
        if (file is not null)
        {
            await using var stream = await file.OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            var fileContent = await streamReader.ReadToEndAsync();
            var project = JsonConvert.DeserializeObject<Project>(fileContent);
        }
    }
}