using Editor.Publisher;
using Engine.Core;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Publisher;

public class GamePublisherValidationTests : IDisposable
{
    private readonly string _tempRoot;

    public GamePublisherValidationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"PublishValidation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public async Task PublishAsync_FailsWhenStartupSceneMissing()
    {
        var projectRoot = Path.Combine(_tempRoot, "project");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets", "scripts"));
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets", "scenes"));

        var context = CreateProjectContext(projectRoot);
        var publisher = new GamePublisher(context);
        var settings = new PublishSettings
        {
            OutputPath = Path.Combine(_tempRoot, "out"),
            RuntimeIdentifier = "win-x64",
            Configuration = "Release"
        };
        var config = new GameConfiguration
        {
            StartupScenePath = "assets/scenes/missing.scene",
            GameAssemblyPath = "GameAssembly.dll",
            GameTitle = "Test"
        };

        var result = await publisher.PublishAsync(settings, config);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull().ShouldContain("Startup scene not found");
    }

    [Fact]
    public async Task PublishAsync_FailsWhenAssetsDirectoryMissing()
    {
        var projectRoot = Path.Combine(_tempRoot, "no-assets");
        Directory.CreateDirectory(projectRoot);
        // ScriptsDir/ScenesDir must be set for ValidateProject, even if folders don't exist on disk.
        var context = Substitute.For<IProjectContext>();
        context.Root.Returns(projectRoot);
        context.ScriptsDir.Returns(Path.Combine(projectRoot, "assets", "scripts"));
        context.ScenesDir.Returns(Path.Combine(projectRoot, "assets", "scenes"));
        context.AssetsPath.Returns(Path.Combine(projectRoot, "assets"));
        context.HasProject.Returns(true);

        File.WriteAllText(Path.Combine(projectRoot, "orphan.scene"), "{}");

        var publisher = new GamePublisher(context);
        var settings = new PublishSettings
        {
            OutputPath = Path.Combine(_tempRoot, "out"),
            RuntimeIdentifier = "win-x64",
            Configuration = "Release"
        };
        var config = new GameConfiguration
        {
            StartupScenePath = "orphan.scene",
            GameAssemblyPath = "GameAssembly.dll",
            GameTitle = "Test"
        };

        var result = await publisher.PublishAsync(settings, config);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull().ShouldContain("Assets directory not found");
    }

    [Fact]
    public void PublishedBuildValidator_FailsWhenExecutableTooSmall()
    {
        var output = Path.Combine(_tempRoot, "build");
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, "Runtime.exe"), new byte[50]);
        File.WriteAllText(Path.Combine(output, "game.config.json"), "{}");
        File.WriteAllBytes(Path.Combine(output, "GameAssembly.dll"), [1]);
        Directory.CreateDirectory(Path.Combine(output, "assets", "scenes"));
        File.WriteAllText(Path.Combine(output, "assets", "scenes", "Scene.scene"), "{}");

        var config = new GameConfiguration
        {
            StartupScenePath = "assets/scenes/Scene.scene",
            GameAssemblyPath = "GameAssembly.dll"
        };

        var result = PublishedBuildValidator.Validate(output, "win-x64", config);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull().ShouldContain("suspiciously small");
    }

    [Fact]
    public void PublishedBuildValidator_SucceedsWhenArtifactsPresent()
    {
        var output = Path.Combine(_tempRoot, "build-ok");
        Directory.CreateDirectory(output);
        File.WriteAllBytes(
            Path.Combine(output, "Runtime.exe"),
            new byte[PublishedBuildValidator.MinimumExecutableBytes]);
        File.WriteAllText(Path.Combine(output, "game.config.json"), "{}");
        File.WriteAllBytes(Path.Combine(output, "GameAssembly.dll"), [1]);
        Directory.CreateDirectory(Path.Combine(output, "assets", "scenes"));
        File.WriteAllText(Path.Combine(output, "assets", "scenes", "Scene.scene"), "{}");

        var config = new GameConfiguration
        {
            StartupScenePath = "assets/scenes/Scene.scene",
            GameAssemblyPath = "GameAssembly.dll"
        };

        var result = PublishedBuildValidator.Validate(output, "win-x64", config);

        result.Success.ShouldBeTrue();
    }

    private static IProjectContext CreateProjectContext(string projectRoot)
    {
        var context = new ProjectContext();
        context.Apply(projectRoot);
        return context;
    }
}
