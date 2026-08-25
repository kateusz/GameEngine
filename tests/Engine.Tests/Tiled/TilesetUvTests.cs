using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Shouldly;

namespace Engine.Tests.Tiled;

public class TilesetUvTests
{
    [Fact]
    public void GetUvRect_Tile0_HasU0ZeroAndV1AboveV0()
    {
        var dest = new Vector2[RenderingConstants.QuadVertexCount];
        TilesetUv.TryGetUvRect(0, 32, 32, 16, 0, 0, false, false, dest).ShouldBeTrue();
        dest[0].X.ShouldBe(0f);
        dest[3].Y.ShouldBeGreaterThan(dest[0].Y);
    }

    [Fact]
    public void GetUvRect_TopRightTile_StaysInUnitSquare()
    {
        var dest = new Vector2[RenderingConstants.QuadVertexCount];
        TilesetUv.TryGetUvRect(1, 32, 32, 16, 0, 0, false, false, dest).ShouldBeTrue();
        dest.ShouldAllBe(v => v.X >= 0 && v.X <= 1 && v.Y >= 0 && v.Y <= 1);
        dest[0].X.ShouldBe(0.5f, 0.0001f);
    }

    [Fact]
    public void Columns_IgnoresRemainderPixels()
    {
        TilesetUv.Columns(40, 16, 0, 0).ShouldBe(2);
        TilesetUv.Rows(32, 16, 0, 0).ShouldBe(2);
    }

    [Fact]
    public void GetUvRect_OutOfRange_ReturnsFalse()
    {
        var dest = new Vector2[4];
        TilesetUv.TryGetUvRect(-1, 32, 32, 16, 0, 0, false, false, dest).ShouldBeFalse();
        TilesetUv.TryGetUvRect(99, 32, 32, 16, 0, 0, false, false, dest).ShouldBeFalse();
    }
}
