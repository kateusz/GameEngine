using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

public class EquirectUvTests
{
    private const float U = 0.1591f;
    private const float V = 0.3183f;

    [Fact]
    public void EquirectUv_MatchesSphericalMap()
    {
        Expect(Vector3.UnitX, 0.5f, 0.5f);
        Expect(-Vector3.UnitX, 0.5f + MathF.Atan2(0f, -1f) * U, 0.5f);
        Expect(Vector3.UnitY, 0.5f, 0.5f + MathF.PI / 2f * V);
        Expect(-Vector3.UnitY, 0.5f, 0.5f - MathF.PI / 2f * V);
        Expect(Vector3.UnitZ, 0.5f + MathF.PI / 2f * U, 0.5f);
        Expect(-Vector3.UnitZ, 0.5f - MathF.PI / 2f * U, 0.5f);
    }

    private static void Expect(Vector3 direction, float u, float v)
    {
        var uv = LightingMath.EquirectUv(direction);
        uv.X.ShouldBe(u, 1e-4f);
        uv.Y.ShouldBe(v, 1e-4f);
    }
}
