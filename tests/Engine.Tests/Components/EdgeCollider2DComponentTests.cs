using System.Numerics;
using SceneComponents.Physics;
using Shouldly;

namespace Engine.Tests.Components;

public class EdgeCollider2DComponentTests
{
    [Fact]
    public void DefaultConstructor_InitializesTwoPoints()
    {
        var component = new EdgeCollider2DComponent();

        component.Points.Count.ShouldBe(2);
        component.Points[0].ShouldBe(new Vector2(-0.5f, 0f));
        component.Points[1].ShouldBe(new Vector2(0.5f, 0f));
        component.Density.ShouldBe(1.0f);
    }

    [Fact]
    public void Clone_CopiesPointsIndependently()
    {
        var original = new EdgeCollider2DComponent
        {
            Points = [new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f)],
            Density = 2f,
            Friction = 0.3f,
            Restitution = 0.4f,
            IsTrigger = true
        };

        var clone = (EdgeCollider2DComponent)original.Clone();

        clone.Points.ShouldBe(original.Points);
        clone.Points.ShouldNotBeSameAs(original.Points);
        clone.Density.ShouldBe(2f);
        clone.IsTrigger.ShouldBeTrue();
    }
}
