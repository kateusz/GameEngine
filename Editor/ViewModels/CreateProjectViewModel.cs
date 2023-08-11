using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace Editor.ViewModels;

public class CreateProjectViewModel : ViewModelBase
{
    public string ProjectName { get; set; }
    public Project Project { get; private set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public CreateProjectViewModel()
    {
        SaveCommand = ReactiveCommand.CreateFromTask(Save);
    }

    private async Task Save()
    {
        // if (!string.IsNullOrEmpty(ProjectName))
        // {
        //     Project = new Project { Name = ProjectName };
        // }
        //
        // // Get top level from the current control. Alternatively, you can use Window reference instead.
        // var topLevel = TopLevel.GetTopLevel(this);
        //
        // // Start async operation to open the dialog.
        // var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        // {
        //     Title = "Save Text File"
        // });
        //
        // if (file is not null)
        // {
        //     // Open writing stream from the file.
        //     await using var stream = await file.OpenWriteAsync();
        //     using var streamWriter = new StreamWriter(stream);
        //     // Write some content to the file.
        //     await streamWriter.WriteLineAsync("Hello World!");
        // }
    }
}