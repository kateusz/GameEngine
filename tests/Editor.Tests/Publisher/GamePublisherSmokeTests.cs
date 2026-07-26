using System.Text.Json;
using Editor.Publisher;
using Engine.Core;
using Shouldly;

namespace Editor.Tests.Publisher;

[Collection("Publisher")]
public class GamePublisherSmokeTests
{
    [Fact]
    public async Task PublishAsync_SnakeProject_ProducesValidPackage()
    {
        var snakeRoot = FindSnakeProjectRoot();
        snakeRoot.ShouldNotBeNull($"Could not find games/Snake under repo (cwd={Environment.CurrentDirectory})");

        var outputPath = Path.Combine(Path.GetTempPath(), $"SnakePublishSmoke_{Guid.NewGuid():N}");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            var context = new ProjectContext();
            context.Apply(snakeRoot!);

            var gameConfig = LoadGameConfig(snakeRoot!);
            var publisher = new GamePublisher(context);
            var settings = new PublishSettings
            {
                OutputPath = outputPath,
                RuntimeIdentifier = "win-x64",
                Configuration = "Release",
                SelfContained = true,
                SingleFile = true
            };

            var result = await publisher.PublishAsync(settings, gameConfig, progress: null, cts.Token);

            result.Success.ShouldBeTrue(result.ErrorMessage ?? "Publish failed without error message");
            result.OutputPath.ShouldBe(outputPath);

            var exePath = Path.Combine(outputPath, "Runtime.exe");
            File.Exists(exePath).ShouldBeTrue($"Missing {exePath}");
            new FileInfo(exePath).Length.ShouldBeGreaterThanOrEqualTo(PublishedBuildValidator.MinimumExecutableBytes);

            File.Exists(Path.Combine(outputPath, "game.config.json")).ShouldBeTrue();
            File.Exists(Path.Combine(outputPath, "GameAssembly.dll")).ShouldBeTrue();
            File.Exists(Path.Combine(outputPath, gameConfig.StartupScenePath)).ShouldBeTrue();
            File.Exists(Path.Combine(outputPath, "assets", "textures", "cell.png")).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, recursive: true);
        }
    }

    private static GameConfiguration LoadGameConfig(string snakeRoot)
    {
        var configPath = Path.Combine(snakeRoot, "game.config.json");
        File.Exists(configPath).ShouldBeTrue($"Missing {configPath}");
        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<GameConfiguration>(json)
               ?? throw new InvalidOperationException("Failed to deserialize Snake game.config.json");
    }

    private static string? FindSnakeProjectRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "games", "Snake");
            if (File.Exists(Path.Combine(candidate, "game.config.json")))
                return candidate;
        }

        var fromCwd = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "games", "Snake"));
        return File.Exists(Path.Combine(fromCwd, "game.config.json")) ? fromCwd : null;
    }
}

/// <summary>
/// Serializes publisher integration tests so smoke publish does not race other publisher fixtures.
/// </summary>
[CollectionDefinition("Publisher", DisableParallelization = true)]
public class PublisherCollection;
