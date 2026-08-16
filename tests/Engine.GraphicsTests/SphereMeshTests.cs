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

        mesh.Vertices.Count.ShouldBe(33 * 17);
        mesh.Indices.Count.ShouldBe(32 * 16 * 6);
        foreach (var v in mesh.Vertices)
        {
            v.Position.Length().ShouldBe(0.5f, 0.001f);
            v.Normal.Length().ShouldBe(1f, 0.001f);
        }
        ReferenceEquals(mesh, fixture.MeshFactory.CreateSphere()).ShouldBeTrue();
    }
}
