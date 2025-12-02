using Engine.Scene.Components;
using Engine.Tiles;

namespace Engine.Scene.Services;

/// <summary>
/// Service responsible for editor-only tilemap editing operations.
/// This centralizes tilemap mutation logic (resize, add/remove layers) outside the component.
/// </summary>
public class TileMapEditingService
{
    /// <summary>
    /// Resizes the tilemap (preserves existing tile data where possible)
    /// </summary>
    public void Resize(TileMapComponent tileMap, int newWidth, int newHeight)
    {
        foreach (var layer in tileMap.Layers)
        {
            var newTiles = new int[newWidth, newHeight];
            for (var x = 0; x < newWidth; x++)
            {
                for (var y = 0; y < newHeight; y++)
                {
                    if (x < tileMap.Width && y < tileMap.Height)
                    {
                        newTiles[x, y] = layer.Tiles[x, y];
                    }
                    else
                    {
                        newTiles[x, y] = -1;
                    }
                }
            }
            layer.SetTiles(newTiles);
        }
        tileMap.SetWidth(newWidth);
        tileMap.SetHeight(newHeight);
    }

    /// <summary>
    /// Adds a new layer to the tilemap
    /// </summary>
    public void AddLayer(TileMapComponent tileMap, string name = "New Layer")
    {
        var layer = new TileMapLayer(tileMap.Width, tileMap.Height) { Name = name, ZIndex = tileMap.Layers.Count };
        tileMap.Layers.Add(layer);
    }

    /// <summary>
    /// Removes a layer at the specified index
    /// </summary>
    public void RemoveLayer(TileMapComponent tileMap, int index)
    {
        if (index >= 0 && index < tileMap.Layers.Count && tileMap.Layers.Count > 1)
        {
            tileMap.Layers.RemoveAt(index);
            if (tileMap.ActiveLayerIndex >= tileMap.Layers.Count)
            {
                tileMap.SetActiveLayerIndex(tileMap.Layers.Count - 1);
            }
        }
    }
}
