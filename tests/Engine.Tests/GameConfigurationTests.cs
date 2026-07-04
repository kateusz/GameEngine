using System.Text.Json;
using Engine.Core;
using Shouldly;

namespace Engine.Tests;

public class GameConfigurationTests
{
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
