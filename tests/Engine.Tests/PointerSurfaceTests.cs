using System.Numerics;
using Engine.Core.Window;
using Shouldly;

namespace Engine.Tests;

public class PointerSurfaceTests
{
    [Fact]
    public void Contains_InsideRect_ReturnsTrue()
    {
        var surface = new PointerSurface();
        surface.Set(new Vector2(10f, 20f), new Vector2(100f, 50f));

        surface.Contains(new Vector2(10f, 20f)).ShouldBeTrue();
        surface.Contains(new Vector2(50f, 40f)).ShouldBeTrue();
        surface.Contains(new Vector2(109.9f, 69.9f)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_OutsideOrEmpty_ReturnsFalse()
    {
        var surface = new PointerSurface();
        surface.Contains(Vector2.Zero).ShouldBeFalse();

        surface.Set(new Vector2(10f, 20f), new Vector2(100f, 50f));
        surface.Contains(new Vector2(9f, 20f)).ShouldBeFalse();
        surface.Contains(new Vector2(110f, 20f)).ShouldBeFalse();
        surface.Contains(new Vector2(10f, 70f)).ShouldBeFalse();
    }
}
