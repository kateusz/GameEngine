using Editor.Features.Project;
using Editor.Features.Scene;
using Editor.Features.Scripting;
using Editor.Features.Settings;
using Engine.Core;
using Engine.Scene.Serializer;
using Engine.Scripting;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Project;

public class ProjectManagerRequiredDirsTests : IDisposable
{
    private readonly string _tempRoot;

    public ProjectManagerRequiredDirsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"RequiredDirs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void TryCreateNewProject_CreatesAssetsModelsDirectory()
    {
        var projectContext = Substitute.For<IProjectContext>();
        projectContext.HasProject.Returns(false);
        projectContext.Root.Returns((string?)null);
        projectContext.ScriptsDir.Returns((string?)null);

        var bootstrapper = Substitute.For<IGameProjectScriptBootstrapper>();
        bootstrapper.TryInstallScriptSdkForNewProject(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
            .Returns(x =>
            {
                x[2] = string.Empty;
                return true;
            });

        var workspace = new GameScriptWorkspace(
            Substitute.For<IScriptEngine>(),
            Substitute.For<IComponentSerializerRegistry>(),
            _ => true,
            _ => { });

        var manager = new ProjectManager(
            Substitute.For<IEditorPreferences>(),
            projectContext,
            workspace,
            bootstrapper,
            Substitute.For<ISceneManager>());

        var ok = manager.TryCreateNewProject(_tempRoot, "DemoProject", out var error);

        ok.ShouldBeTrue(error);
        Directory.Exists(Path.Combine(_tempRoot, "DemoProject", "assets", "models")).ShouldBeTrue();
    }
}
