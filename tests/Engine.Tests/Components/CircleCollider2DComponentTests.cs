using System.Numerics;
using SceneComponents.Physics;
using Shouldly;

namespace Engine.Tests.Components;

public class CircleCollider2DComponentTests
{
    [Fact]
    public void DefaultConstructor_InitializesDefaults()
    {
        var component = new CircleCollider2DComponent();

        component.Radius.ShouldBe(0.5f);
        component.Offset.ShouldBe(Vector2.Zero);
        component.Density.ShouldBe(1.0f);
        component.Friction.ShouldBe(0.5f);
        component.Restitution.ShouldBe(0.7f);
        component.IsTrigger.ShouldBeFalse();
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        var original = new CircleCollider2DComponent
        {
            Radius = 1.5f,
            Offset = new Vector2(1f, 2f),
            Density = 3f,
            Friction = 0.4f,
            Restitution = 0.6f,
            IsTrigger = true
        };

        var clone = (CircleCollider2DComponent)original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Radius.ShouldBe(original.Radius);
        clone.Offset.ShouldBe(original.Offset);
        clone.Density.ShouldBe(original.Density);
        clone.Friction.ShouldBe(original.Friction);
        clone.Restitution.ShouldBe(original.Restitution);
        clone.IsTrigger.ShouldBe(original.IsTrigger);
    }
}
