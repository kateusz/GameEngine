namespace Engine.Scene.Components;

/// <summary>
/// Represents a layer of tiles in a tilemap
/// </summary>
public class TileMapLayer
{
    public string Name { get; set; } = "Layer";
    public bool Visible { get; set; } = true;
    public float Opacity { get; set; } = 1.0f;
    public int ZIndex { get; set; } = 0;
    
    // Tile data: -1 = empty, >= 0 = tile index in tileset
    public int[,] Tiles { get; set; }
    
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
}