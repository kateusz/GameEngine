using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Tiled;

public class TileMapComponentTests
{
    [Fact]
    public void SetTile_OutOfRange_LeavesArray()
    {
        var layer = new TileMapLayer { Tiles = [-1, -1, -1, -1], Flags = new byte[4] };
        layer.SetTile(2, 2, 9, 0, 1);
        layer.SetTile(2, 2, 0, 0, -2);
        layer.Tiles.ShouldAllBe(t => t == -1);
        layer.SetTile(2, 2, 0, 0, 3);
        layer.Tiles[0].ShouldBe(3);
    }

    [Fact]
    public void Repair_PadsAndTruncates()
    {
        var map = new TileMapComponent { Width = 2, Height = 2 };
        map.Layers.Add(new TileMapLayer { Tiles = [1], Flags = [0] });
        map.Repair();
        map.Layers[0].Tiles.Length.ShouldBe(4);
        map.Layers[0].Tiles[0].ShouldBe(1);
        map.Layers[0].Tiles[1].ShouldBe(-1);
    }
}
