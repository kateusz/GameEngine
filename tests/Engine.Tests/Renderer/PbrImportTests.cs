using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class PbrImportTests
{
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
