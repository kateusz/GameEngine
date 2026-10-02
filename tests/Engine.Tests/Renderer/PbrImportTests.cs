using System.Numerics;
using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class PbrImportTests
{
    [Fact]
    public void ImportSource_GltfPbrFactors_LandOnSubmesh()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "PbrFactors.gltf");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine("tests", "Engine.Tests", "TestAssets", "PbrFactors.gltf"));

        using var importer = new AssimpModelImporter();
        var model = importer.ImportSource(path);
        model.ShouldNotBeNull();
        model!.Submeshes.Count.ShouldBe(1);

        var mesh = model.Submeshes[0];
        mesh.Metallic.ShouldBe(0.85f, 0.001f);
        mesh.Roughness.ShouldBe(0.2f, 0.001f);
        mesh.BaseColorFactor.X.ShouldBe(1f, 0.001f);
        mesh.BaseColorFactor.Y.ShouldBe(0.5f, 0.001f);
        mesh.BaseColorFactor.Z.ShouldBe(0.1f, 0.001f);
        mesh.AlphaCutout.ShouldBeFalse();
    }

    [Fact]
    public void ImportSource_GltfBlend_IsDoubleSidedCutout()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "AlphaLeaf.gltf");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine("tests", "Engine.Tests", "TestAssets", "AlphaLeaf.gltf"));

        using var importer = new AssimpModelImporter();
        var model = importer.ImportSource(path);
        model.ShouldNotBeNull();
        var mesh = model!.Submeshes[0];
        mesh.AlphaCutout.ShouldBeTrue();
        mesh.DoubleSided.ShouldBeTrue();
        mesh.AlphaCutoff.ShouldBe(0.5f, 0.001f);
    }

    [Fact]
    public void UsesAlphaCutout_MaskAndBlend()
    {
        AssimpModelImporter.UsesAlphaCutout("MASK").ShouldBeTrue();
        AssimpModelImporter.UsesAlphaCutout("BLEND").ShouldBeTrue();
        AssimpModelImporter.UsesAlphaCutout("OPAQUE").ShouldBeFalse();
        AssimpModelImporter.UsesAlphaCutout(null).ShouldBeFalse();
    }

    [Fact]
    public void ChooseMetallicRoughnessPath_PackedWins()
    {
        AssimpModelImporter.ChooseMetallicRoughnessPath("packed.png", "metal.png", "rough.png")
            .ShouldBe("packed.png");
    }

    [Fact]
    public void ChooseMetallicRoughnessPath_MetalnessWinsWhenPathsDiffer()
    {
        AssimpModelImporter.ChooseMetallicRoughnessPath(null, "metal.png", "rough.png")
            .ShouldBe("metal.png");
    }

    [Fact]
    public void ChooseMetallicRoughnessPath_SameSeparatePaths_ReturnsThatPath()
    {
        AssimpModelImporter.ChooseMetallicRoughnessPath(null, "orm.png", "orm.png")
            .ShouldBe("orm.png");
    }

    [Fact]
    public void ImportedFactor_MissingOrNonFinite_UsesDefault()
    {
        AssimpModelImporter.ImportedFactor(false, 1f, 0f).ShouldBe(0f);
        AssimpModelImporter.ImportedFactor(true, float.NaN, 0.5f).ShouldBe(0.5f);
    }

    [Fact]
    public void ImportedFactor_PresentValue_Clamps()
    {
        AssimpModelImporter.ImportedFactor(true, 1f, 0f).ShouldBe(1f);
        AssimpModelImporter.ImportedFactor(true, 2f, 0f).ShouldBe(1f);
    }
}
