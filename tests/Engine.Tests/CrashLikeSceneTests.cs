using System.Text.Json;
using Shouldly;

namespace Engine.Tests;

public class CrashLikeSceneTests
{
    [Fact]
    public void LevelScene_ReferencesRealTexturesAndThePlayer()
    {
        var root = FindRepoFile("games/CrashLike");
        var scenePath = Path.Combine(root, "assets", "scenes", "level01.scene");
        var scene = JsonDocument.Parse(File.ReadAllText(scenePath));
        var entities = scene.RootElement.GetProperty("Entities");

        var names = new HashSet<string>();
        var ids = new HashSet<int>();
        var textures = new HashSet<string>();
        foreach (var entity in entities.EnumerateArray())
        {
            var name = entity.GetProperty("Name").GetString();
            name.ShouldNotBeNullOrWhiteSpace();
            names.Add(name!).ShouldBeTrue($"duplicate name {name}");
            ids.Add(entity.GetProperty("Id").GetInt32()).ShouldBeTrue();

            foreach (var component in entity.GetProperty("Components").EnumerateArray())
            {
                if (component.TryGetProperty("TexturePath", out var texture) && texture.ValueKind == JsonValueKind.String)
                {
                    var path = texture.GetString();
                    if (!string.IsNullOrWhiteSpace(path))
                        textures.Add(path);
                }
            }
        }

        scene.RootElement.GetProperty("Dimension").GetString().ShouldBe("ThreeD");
        names.ShouldContain("Player");
        names.ShouldContain("Camera");
        names.ShouldContain("Game");
        names.ShouldContain("Goal");
        names.ShouldContain("Sun");
        names.ShouldContain("ZoneSide");

        foreach (var texture in textures)
        {
            File.Exists(Path.Combine(root, "assets", texture)).ShouldBeTrue(texture);
        }

        foreach (var sound in new[]
                 {
                     "jump.ogg", "spin.ogg", "crate.ogg", "wumpa.ogg", "hurt.ogg", "mask.ogg",
                     "die.ogg", "checkpoint.ogg", "win.ogg", "life.ogg", "bounce.ogg", "stomp.ogg", "fuse.ogg"
                 })
        {
            File.Exists(Path.Combine(root, "assets", "sounds", sound)).ShouldBeTrue(sound);
        }
    }

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(relative);
    }
}
