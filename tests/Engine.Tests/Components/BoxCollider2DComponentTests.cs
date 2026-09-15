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
        component.IsTrigger.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_Clone_ShouldCopyAllProperties()
    {
        var original = new BoxCollider2DComponent
        {
            Size = new Vector2(10f, 10f),
            Offset = new Vector2(1f, 1f),
            Density = 3.0f,
            Friction = 0.6f,
            Restitution = 0.4f,
            IsTrigger = true
        };

        var clone = (BoxCollider2DComponent)original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Size.ShouldBe(original.Size);
        clone.Offset.ShouldBe(original.Offset);
        clone.Density.ShouldBe(original.Density);
        clone.Friction.ShouldBe(original.Friction);
        clone.Restitution.ShouldBe(original.Restitution);
        clone.IsTrigger.ShouldBe(original.IsTrigger);
    }
}
