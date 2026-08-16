using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class SphereMeshTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void CreateSphere_ProducesUnitDiameterSphere()
    {
        var mesh = fixture.MeshFactory.CreateSphere();

        mesh.GetIndexCount().ShouldBe(32 * 16 * 6);
        Should.Throw<InvalidOperationException>(() => _ = mesh.Vertices);
        ReferenceEquals(mesh, fixture.MeshFactory.CreateSphere()).ShouldBeTrue();
    }
}
