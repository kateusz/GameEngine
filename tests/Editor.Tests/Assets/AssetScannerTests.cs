using Editor.AssetPicker;
using Shouldly;

namespace Editor.Tests.Assets;

public class AssetScannerTests
{
    [Fact]
    public void Scan_filters_by_extension_and_skips_bin()
    {
        var root = CreateTempProject();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "assets", "models"));
            Directory.CreateDirectory(Path.Combine(root, "bin", "Debug"));
            File.WriteAllText(Path.Combine(root, "assets", "models", "hero.fbx"), "x");
            File.WriteAllText(Path.Combine(root, "assets", "models", "readme.txt"), "x");
            File.WriteAllText(Path.Combine(root, "bin", "Debug", "junk.fbx"), "x");

            var found = AssetScanner.Scan(root, AssetKind.Model.Extensions);

            found.Count.ShouldBe(1);
            found[0].DisplayPath.ShouldBe("assets/models/hero.fbx");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Scan_always_skips_addons()
    {
        var root = CreateTempProject();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "addons", "pack"));
            File.WriteAllText(Path.Combine(root, "addons", "pack", "a.png"), "x");
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            File.WriteAllText(Path.Combine(root, "assets", "b.png"), "x");

            AssetScanner.Scan(root, AssetKind.Texture.Extensions).Count.ShouldBe(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("assets/models/hero.fbx", "hr", true, true)]
    [InlineData("assets/models/hero.fbx", "xyz", true, false)]
    [InlineData("assets/models/hero.fbx", "hero", false, true)]
    [InlineData("assets/models/hero.fbx", "models", false, false)]
    public void MatchesSearch(string path, string query, bool fuzzy, bool expected) =>
        AssetScanner.MatchesSearch(path, query, fuzzy).ShouldBe(expected);

    private static string CreateTempProject()
    {
        var root = Path.Combine(Path.GetTempPath(), "ge-asset-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
