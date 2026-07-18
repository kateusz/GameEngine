using System.Numerics;
using Engine.Core.Input;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.Tests;

public class ScreenWorldConverterTests
{
    [Fact]
    public void ScreenToWorld2D_ZeroSurface_ReturnsNull()
    {
        var vp = OrthoViewProjection(size: 5f, aspect: 16f / 9f);

        ScreenWorldConverter.ScreenToWorld2D(Vector2.Zero, Vector2.Zero, Vector2.Zero, vp)
            .ShouldBeNull();
    }

    [Fact]
    public void ScreenToWorld2D_CenterOfSurface_MapsNearOrigin()
    {
        var origin = new Vector2(100f, 50f);
        var size = new Vector2(800f, 450f);
        var center = origin + size * 0.5f;
        var vp = OrthoViewProjection(size: 5f, aspect: size.X / size.Y);

        var world = ScreenWorldConverter.ScreenToWorld2D(center, origin, size, vp);

        world.ShouldNotBeNull();
        world!.Value.X.ShouldBe(0f, 0.05f);
        world.Value.Y.ShouldBe(0f, 0.05f);
    }

    [Fact]
    public void ScreenToWorld2D_TopCenter_MapsTowardPositiveY()
    {
        // Top of surface (small Y in window space) → positive world Y after Y flip.
        var origin = new Vector2(0f, 0f);
        var size = new Vector2(800f, 450f);
        var topCenter = new Vector2(400f, 0f);
        var vp = OrthoViewProjection(size: 5f, aspect: size.X / size.Y);

        var world = ScreenWorldConverter.ScreenToWorld2D(topCenter, origin, size, vp);

        world.ShouldNotBeNull();
        world!.Value.Y.ShouldBeGreaterThan(2f);
    }

    private static Matrix4x4 OrthoViewProjection(float size, float aspect)
    {
        var orthoLeft = -size * aspect;
        var orthoRight = size * aspect;
        var orthoBottom = -size;
        var orthoTop = size;
        var projection = Matrix4x4.CreateOrthographicOffCenter(orthoLeft, orthoRight, orthoBottom, orthoTop, -1f, 1f);
        // Identity camera transform → view = identity.
        return projection;
    }
}
