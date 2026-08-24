using System.Numerics;
using SceneComponents.Physics;
using Shouldly;

namespace Engine.Tests.Components;

public class BoxCollider2DComponentTests
{
    [Fact]
    public void BoxCollider2DComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var component = new BoxCollider2DComponent();

        component.Size.ShouldBe(new Vector2(0.5f, 0.5f));
        component.Offset.ShouldBe(Vector2.Zero);
        component.Density.ShouldBe(1.0f);
        component.Friction.ShouldBe(0.5f);
        component.Restitution.ShouldBe(0.7f);
        component.RestitutionThreshold.ShouldBe(0.5f);
        component.IsTrigger.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_ParameterizedConstructor_ShouldSetAllProperties()
    {
        var size = new Vector2(2f, 3f);
        var offset = new Vector2(0.5f, 0.5f);
        var revisionBefore = PhysicsBodyRevision.Value;

        var component = new BoxCollider2DComponent(size, offset, 2.0f, 0.3f, 0.8f, 1.0f, true);

        component.Size.ShouldBe(size);
        component.Offset.ShouldBe(offset);
        component.Density.ShouldBe(2.0f);
        component.Friction.ShouldBe(0.3f);
        component.Restitution.ShouldBe(0.8f);
        component.RestitutionThreshold.ShouldBe(1.0f);
        component.IsTrigger.ShouldBeTrue();
        PhysicsBodyRevision.Value.ShouldBeGreaterThan(revisionBefore);
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ShouldBumpRevision()
    {
        var component = new BoxCollider2DComponent();
        var revisionBefore = PhysicsBodyRevision.Value;

        component.Density = 5.0f;

        component.Density.ShouldBe(5.0f);
        PhysicsBodyRevision.Value.ShouldBe(revisionBefore + 1);
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ToSameValue_ShouldNotBumpRevision()
    {
        var component = new BoxCollider2DComponent();
        component.Density = 3.0f;
        var revisionBefore = PhysicsBodyRevision.Value;

        component.Density = 3.0f;

        PhysicsBodyRevision.Value.ShouldBe(revisionBefore);
    }

    [Fact]
    public void BoxCollider2DComponent_SetFriction_ShouldNotBumpRevision()
    {
        var component = new BoxCollider2DComponent();
        var revisionBefore = PhysicsBodyRevision.Value;

        component.Friction = 0.8f;

        component.Friction.ShouldBe(0.8f);
        PhysicsBodyRevision.Value.ShouldBe(revisionBefore);
    }

    [Fact]
    public void BoxCollider2DComponent_SetRestitution_ShouldNotBumpRevision()
    {
        var component = new BoxCollider2DComponent();
        var revisionBefore = PhysicsBodyRevision.Value;

        component.Restitution = 0.9f;

        component.Restitution.ShouldBe(0.9f);
        PhysicsBodyRevision.Value.ShouldBe(revisionBefore);
    }

    [Fact]
    public void BoxCollider2DComponent_Clone_ShouldCopyAllProperties()
    {
        var original = new BoxCollider2DComponent(
            new Vector2(10f, 10f),
            new Vector2(1f, 1f),
            3.0f, 0.6f, 0.4f, 0.8f, true);

        var clone = (BoxCollider2DComponent)original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Size.ShouldBe(original.Size);
        clone.Offset.ShouldBe(original.Offset);
        clone.Density.ShouldBe(original.Density);
        clone.Friction.ShouldBe(original.Friction);
        clone.Restitution.ShouldBe(original.Restitution);
        clone.RestitutionThreshold.ShouldBe(original.RestitutionThreshold);
        clone.IsTrigger.ShouldBe(original.IsTrigger);
    }
}
