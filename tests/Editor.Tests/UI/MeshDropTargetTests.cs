using Editor.UI.Elements;
using Shouldly;

namespace Editor.Tests.UI;

public class MeshDropTargetTests
{
    [Fact]
    public void IsSupported_AcceptsMeshExtension()
    {
        MeshDropTarget.IsSupported("hero.mesh").ShouldBeTrue();
        MeshDropTarget.IsSupported("Hero.MESH").ShouldBeTrue();
    }

    [Theory]
    [InlineData("hero.glb")]
    [InlineData("hero.fbx")]
    [InlineData("hero.gltf")]
    [InlineData("hero.obj")]
    public void IsSupported_RejectsSourceFormats(string filename)
    {
        MeshDropTarget.IsSupported(filename).ShouldBeFalse();
    }

    [Fact]
    public void SupportedExtensions_IsMeshOnly()
    {
        MeshDropTarget.SupportedExtensions.ShouldBe([".mesh"]);
    }
}
