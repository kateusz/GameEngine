namespace Engine.Tiles;

/// <summary>
/// Represents a layer of tiles in a tilemap
/// </summary>
public class TileMapLayer
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;
    public required int ZIndex { get; set; }
    
    // Tile data: -1 = empty, >= 0 = tile index in tileset
    public int[,] Tiles { get; private set; }
    
    public TileMapLayer(int width, int height)
    {
        Tiles = new int[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Tiles[x, y] = -1;
            }
        }
    }

    public void SetTiles(int[,] tiles) => Tiles = tiles;
    public void SetName(string name) => Name = name;
}