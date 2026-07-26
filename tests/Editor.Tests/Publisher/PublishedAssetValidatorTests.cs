using Editor.Publisher;
using Shouldly;

namespace Editor.Tests.Publisher;

public class PublishedAssetValidatorTests : IDisposable
{
    private readonly string _tempRoot;

    public PublishedAssetValidatorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"AssetValidator_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void ValidateAssetsDirectory_FailsWhenAssetsFolderMissing()
    {
        var result = PublishedAssetValidator.ValidateAssetsDirectory(_tempRoot);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull().ShouldContain("Assets directory not found");
    }

    [Fact]
    public void ValidateAssetsDirectory_SucceedsWhenAssetsFolderExists()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "assets"));

        var result = PublishedAssetValidator.ValidateAssetsDirectory(_tempRoot);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAssetReferences_FailsWhenTexturePathMissing()
    {
        var assets = CreateAssetsLayout();
        WriteScene(assets, """
            {
              "Entities": [
                {
                  "Components": [
                    { "TexturePath": "textures/missing.png" }
                  ]
                }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeFalse();
        var error = result.ErrorMessage.ShouldNotBeNull();
        error.ShouldContain("textures/missing.png");
        error.ShouldContain("TexturePath");
    }

    [Fact]
    public void ValidateAssetReferences_SucceedsWhenTextureExists()
    {
        var assets = CreateAssetsLayout();
        var textureDir = Path.Combine(assets, "textures");
        Directory.CreateDirectory(textureDir);
        File.WriteAllBytes(Path.Combine(textureDir, "cell.png"), [1, 2, 3]);
        WriteScene(assets, """
            {
              "Entities": [
                {
                  "Components": [
                    { "TexturePath": "textures/cell.png" }
                  ]
                }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAssetReferences_IgnoresEmptyPath()
    {
        var assets = CreateAssetsLayout();
        WriteScene(assets, """
            {
              "Entities": [
                {
                  "Components": [
                    { "TexturePath": "" },
                    { "TexturePath": null }
                  ]
                }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAssetReferences_StripsAssetsPrefix()
    {
        var assets = CreateAssetsLayout();
        var textureDir = Path.Combine(assets, "textures");
        Directory.CreateDirectory(textureDir);
        File.WriteAllBytes(Path.Combine(textureDir, "cell.png"), [1, 2, 3]);
        WriteScene(assets, """
            {
              "Components": [
                { "TexturePath": "assets/textures/cell.png" }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAssetReferences_ChecksPrefabsAndAudioAndModelPaths()
    {
        var assets = CreateAssetsLayout();
        WritePrefab(assets, """
            {
              "Components": [
                { "AudioClipPath": "sounds/missing.wav" },
                { "ModelPath": "models/missing.mesh" }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeFalse();
        var error = result.ErrorMessage.ShouldNotBeNull();
        error.ShouldContain("sounds/missing.wav");
        error.ShouldContain("models/missing.mesh");
    }

    [Fact]
    public void ValidateAssetReferences_SucceedsWhenCookedMeshExists()
    {
        var assets = CreateAssetsLayout();
        Directory.CreateDirectory(Path.Combine(assets, "models"));
        File.WriteAllBytes(Path.Combine(assets, "models", "crate.mesh"), [1, 2, 3]);
        WriteScene(assets, """
            {
              "Entities": [
                {
                  "Components": [
                    { "ModelPath": "models/crate.mesh" }
                  ]
                }
              ]
            }
            """);

        var result = PublishedAssetValidator.ValidateAssetReferences(assets);

        result.Success.ShouldBeTrue();
    }

    private string CreateAssetsLayout()
    {
        var assets = Path.Combine(_tempRoot, "assets");
        Directory.CreateDirectory(Path.Combine(assets, "scenes"));
        Directory.CreateDirectory(Path.Combine(assets, "prefabs"));
        return assets;
    }

    private static void WriteScene(string assets, string json) =>
        File.WriteAllText(Path.Combine(assets, "scenes", "test.scene"), json);

    private static void WritePrefab(string assets, string json) =>
        File.WriteAllText(Path.Combine(assets, "prefabs", "test.prefab"), json);
}
