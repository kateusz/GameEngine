using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class AssimpTexturePathTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "AssimpTexturePathTests-" + Guid.NewGuid().ToString("N"));

    public AssimpTexturePathTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void Resolve_swaps_tga_for_png_beside_the_model()
    {
        var png = Touch("T_Doors_N.png");

        AssimpTexturePath.Resolve(@"C:\ghost\T_Doors_N.tga", _dir).ShouldBe(png);
        AssimpTexturePath.Resolve("/nowhere/T_Doors_N.tga", _dir).ShouldBe(png);
    }

    [Fact]
    public void Resolve_adds_unreal_T_prefix()
    {
        var png = Touch("T_Backsplash_D.png");

        AssimpTexturePath.Resolve(
            @"D:\Phillip\Documents\Textures\Backsplash_D.tga", _dir).ShouldBe(png);
    }

    [Fact]
    public void Resolve_maps_FlatNormal_to_T_Default_N()
    {
        var png = Touch("T_Default_N.png");

        AssimpTexturePath.Resolve(@"..\..\..\jonathan.lindquist\Desktop\FlatNormal.tga", _dir)
            .ShouldBe(png);
    }

    [Fact]
    public void InferAlbedoFromNormal_prefers_D_then_BC()
    {
        Touch("T_Food_BC.png");
        var diffuse = Touch("T_Food_D.png");

        AssimpTexturePath.InferAlbedoFromNormal(Path.Combine(_dir, "T_Food_N.png"), _dir)
            .ShouldBe(diffuse);
    }

    [Fact]
    public void InferAlbedoFromNormal_uses_BC_when_D_is_missing()
    {
        var bc = Touch("T_LeatherCouch_BC.png");

        AssimpTexturePath.InferAlbedoFromNormal(@"D:\Unreal\T_LeatherCouch_N.TGA", _dir)
            .ShouldBe(bc);
        AssimpTexturePath.InferAlbedoFromNormal("/nowhere/T_LeatherCouch_N.TGA", _dir)
            .ShouldBe(bc);
    }

    [Fact]
    public void Resolve_relative_backslashes_prefer_the_named_subdir()
    {
        Touch("exact.tga");
        var nested = Touch(Path.Combine("sub", "exact.tga"));

        AssimpTexturePath.Resolve(@"sub\exact.tga", _dir).ShouldBe(nested);
    }

    [Fact]
    public void InferAlbedoFromNormal_searches_beside_a_native_normal_path()
    {
        var other = Path.Combine(_dir, "maps");
        Directory.CreateDirectory(other);
        var albedo = Path.GetFullPath(Path.Combine(other, "T_Crate_D.png"));
        File.WriteAllBytes(albedo, [1]);

        AssimpTexturePath.InferAlbedoFromNormal(Path.Combine(other, "T_Crate_N.png"), _dir)
            .ShouldBe(albedo);
    }

    [Fact]
    public void IsUnrealCollisionMesh_detects_ucx_prefix()
    {
        AssimpModelImporter.IsUnrealCollisionMesh("UCX_Cube3").ShouldBeTrue();
        AssimpModelImporter.IsUnrealCollisionMesh("UBX_Box").ShouldBeTrue();
        AssimpModelImporter.IsUnrealCollisionMesh("SM_Chair").ShouldBeFalse();
        AssimpModelImporter.IsUnrealCollisionMesh(null).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private string Touch(string name)
    {
        var path = Path.Combine(_dir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [1]);
        return Path.GetFullPath(path);
    }
}
