using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Shouldly;

namespace Engine.Tests.Renderer;

/// <summary>
/// Pins the shadow-pass instance packing: depth.vert reads only a_InstanceModel, so packing a
/// shadow instance must not compute the inverse-transpose normal matrix (one Matrix4x4.Invert per
/// instance per pass). If a depth shader ever needs world normals, delete this test with the change.
/// </summary>
[Trait("Category", "Unit")]
public class Graphics3DInstancePackingTests
{
    static readonly MeshDrawInstance Instance = new()
    {
        Transform = Matrix4x4.CreateScale(2f, 3f, 4f) * Matrix4x4.CreateTranslation(5f, 6f, 7f),
        EntityId = 42
    };

    [Fact]
    public void ShadowInstance_carries_model_and_entity_but_skips_the_normal_matrix()
    {
        var packed = Graphics3D.PackShadowInstance(Instance);
        var color = Graphics3D.PackInstance(Instance);

        packed.Model.ShouldBe(color.Model);
        packed.EntityId.ShouldBe(42);
        // Unwritten, so all zeros. Safe only because depth.vert never declares layout(location = 10).
        packed.Normal.ShouldBe(default);
        // Guard the assertion itself: a real packed normal matrix is never all zeros.
        color.Normal.ShouldNotBe(default);
    }
}
