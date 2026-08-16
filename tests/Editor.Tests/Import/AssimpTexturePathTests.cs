using Editor.Features.Import;
using Shouldly;

namespace Editor.Tests.Import;

[Trait("Category", "Unit")]
public class AssimpTexturePathTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "AssimpTexturePathTests_" + Guid.NewGuid().ToString("N"));

    public AssimpTexturePathTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
        }
    }

    [Theory]
    [InlineData("E:/Dropbox/art/T_wood.png")]
    [InlineData(@"D:\Dropbox\art\T_wood.png")]
    [InlineData("/Users/me/textures/wood.png")]
    public void IsAbsolute_RecognizesDriveAndUnixRoots(string path) =>
        AssimpTexturePath.IsAbsolute(path).ShouldBeTrue();

    [Theory]
    [InlineData("textures/wood.png")]
    [InlineData("./wood.png")]
    [InlineData("wood.png")]
    public void IsAbsolute_FalseForRelative(string path) =>
        AssimpTexturePath.IsAbsolute(path).ShouldBeFalse();

    [Fact]
    public void Resolve_DoesNotPrefixModelDirOntoWindowsAbsolutePath()
    {
        var modelDir = Path.Combine(_root, "buildings");
        Directory.CreateDirectory(modelDir);
        // Texture lives beside the FBX under a basename search, not at E:/…
        var tex = Path.Combine(modelDir, "T_wood_05_BC.png");
        File.WriteAllBytes(tex, [1]);

        var resolved = AssimpTexturePath.Resolve(
            "E:/Dropbox/Tidal Flask Studios/art/exp/2d/FANTASTIC/T_wood_05_BC.png",
            modelDir);

        resolved.ShouldBe(Path.GetFullPath(tex));
        resolved!.ShouldNotContain("E:");
        resolved.ShouldNotContain(Path.Combine(modelDir, "E:"));
    }

    [Fact]
    public void Resolve_FindsBasenameUnderNestedFolder()
    {
        var modelDir = Path.Combine(_root, "buildings");
        var nested = Path.Combine(modelDir, "maps");
        Directory.CreateDirectory(nested);
        var tex = Path.Combine(nested, "T_rooftiles_01_BC.png");
        File.WriteAllBytes(tex, [2]);

        var resolved = AssimpTexturePath.Resolve(
            @"D:\Dropbox\art\T_rooftiles_01_BC.png",
            modelDir);

        resolved.ShouldBe(Path.GetFullPath(tex));
    }

    [Fact]
    public void Resolve_RelativePath_CombinesWithModelDirectory()
    {
        var modelDir = Path.Combine(_root, "buildings");
        var nested = Path.Combine(modelDir, "tex");
        Directory.CreateDirectory(nested);
        var tex = Path.Combine(nested, "wood.png");
        File.WriteAllBytes(tex, [3]);

        var resolved = AssimpTexturePath.Resolve("tex/wood.png", modelDir);

        resolved.ShouldBe(Path.GetFullPath(tex));
    }

    [Fact]
    public void Resolve_Missing_ReturnsNull()
    {
        var modelDir = Path.Combine(_root, "empty");
        Directory.CreateDirectory(modelDir);

        AssimpTexturePath.Resolve("E:/nowhere/missing.png", modelDir).ShouldBeNull();
    }
}
