using System.Text.Json;
using Engine.Core;
using Engine.Project;
using Shouldly;

namespace Engine.Tests;

public class GameConfigurationTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"GameConfig_{Guid.NewGuid():N}");

    public GameConfigurationTests() => Directory.CreateDirectory(_tempRoot);

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void PathFor_joins_directory_and_file_name()
    {
        GameConfiguration.PathFor("games").ShouldBe(Path.Combine("games", GameConfiguration.FileName));
    }

    [Fact]
    public void ForNewProject_sets_title_and_scene_keeps_type_defaults()
    {
        var config = GameConfiguration.ForNewProject("DemoProject", "assets/scenes/DemoProject.scene");

        config.GameTitle.ShouldBe("DemoProject");
        config.StartupScenePath.ShouldBe("assets/scenes/DemoProject.scene");
        config.GameAssemblyPath.ShouldBe("GameAssembly.dll");
        config.WindowWidth.ShouldBe(1920);
        config.WindowHeight.ShouldBe(1080);
        config.Fullscreen.ShouldBeFalse();
        config.TargetFrameRate.ShouldBe(60);
    }

    [Fact]
    public void Save_TryLoad_round_trips_runtime_fields()
    {
        var path = GameConfiguration.PathFor(_tempRoot);
        var original = new GameConfiguration
        {
            GameAssemblyPath = "GameAssembly.dll",
            StartupScenePath = "assets/scenes/level1.scene",
            WindowWidth = 1280,
            WindowHeight = 720,
            Fullscreen = true,
            GameTitle = "Test Game",
            TargetFrameRate = 120
        };

        GameConfiguration.Save(path, original);

        GameConfiguration.TryLoad(path, out var restored, out var error).ShouldBeTrue(error);
        restored.ShouldNotBeNull();
        restored.GameAssemblyPath.ShouldBe(original.GameAssemblyPath);
        restored.StartupScenePath.ShouldBe(original.StartupScenePath);
        restored.WindowWidth.ShouldBe(original.WindowWidth);
        restored.WindowHeight.ShouldBe(original.WindowHeight);
        restored.Fullscreen.ShouldBe(original.Fullscreen);
        restored.GameTitle.ShouldBe(original.GameTitle);
        restored.TargetFrameRate.ShouldBe(original.TargetFrameRate);
    }

    [Fact]
    public void TryLoad_fails_when_file_missing()
    {
        var path = GameConfiguration.PathFor(_tempRoot);

        GameConfiguration.TryLoad(path, out var config, out var error).ShouldBeFalse();
        config.ShouldBeNull();
        error.ShouldNotBeNull().ShouldContain("not found");
    }

    [Fact]
    public void TryLoad_fails_on_invalid_json()
    {
        var path = GameConfiguration.PathFor(_tempRoot);
        File.WriteAllText(path, "{ not json");

        GameConfiguration.TryLoad(path, out var config, out var error).ShouldBeFalse();
        config.ShouldBeNull();
        error.ShouldNotBeNull().ShouldContain("Failed to load");
    }

    [Fact]
    public void Json_round_trip_preserves_runtime_fields()
    {
        var original = new GameConfiguration
        {
            GameAssemblyPath = "GameAssembly.dll",
            StartupScenePath = "assets/scenes/level1.scene",
            WindowWidth = 1280,
            WindowHeight = 720,
            Fullscreen = true,
            GameTitle = "Test Game",
            TargetFrameRate = 120
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GameConfiguration>(json);

        restored.ShouldNotBeNull();
        restored.GameAssemblyPath.ShouldBe(original.GameAssemblyPath);
        restored.StartupScenePath.ShouldBe(original.StartupScenePath);
        restored.WindowWidth.ShouldBe(original.WindowWidth);
        restored.WindowHeight.ShouldBe(original.WindowHeight);
        restored.Fullscreen.ShouldBe(original.Fullscreen);
        restored.GameTitle.ShouldBe(original.GameTitle);
        restored.TargetFrameRate.ShouldBe(original.TargetFrameRate);
    }

    [Fact]
    public void Json_deserialize_ignores_removed_legacy_fields()
    {
        const string json = """
            {
              "GameAssemblyPath": "GameAssembly.dll",
              "StartupScenePath": "assets/scenes/game.scene",
              "WindowWidth": 1920,
              "WindowHeight": 1080,
              "GameTitle": "Legacy Game",
              "Fullscreen": true,
              "TargetFrameRate": 120
            }
            """;

        var config = JsonSerializer.Deserialize<GameConfiguration>(json);

        config.ShouldNotBeNull();
        config.GameTitle.ShouldBe("Legacy Game");
        config.Fullscreen.ShouldBeTrue();
        config.TargetFrameRate.ShouldBe(120);
    }
}
