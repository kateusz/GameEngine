using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class LightingMathTests
{
    [Fact]
    public void NormalizeDirection_UnitVector_ReturnsSameDirection()
    {
        var direction = new Vector3(1, 0, 0);

        LightingMath.NormalizeDirection(direction).ShouldBe(direction);
    }

    [Fact]
    public void NormalizeDirection_ZeroVector_ReturnsDefaultDown()
    {
        LightingMath.NormalizeDirection(Vector3.Zero).ShouldBe(LightingMath.DefaultDirection);
    }

    [Fact]
    public void NormalizeDirection_NearZeroVector_ReturnsDefaultDown()
    {
        var direction = new Vector3(1e-7f, 0f, 0f);

        LightingMath.NormalizeDirection(direction).ShouldBe(LightingMath.DefaultDirection);
    }
}
