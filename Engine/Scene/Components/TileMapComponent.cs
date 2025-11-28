using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

/// <summary>
/// Component for storing and managing tilemap data
/// </summary>
public class TileMapComponent : IComponent
{
    /// <summary>
    /// Width of the tilemap in tiles
    /// </summary>
    public int Width { get; set; } = 16;
    
    /// <summary>
    /// Height of the tilemap in tiles
    /// </summary>
    public int Height { get; set; } = 16;
    
    /// <summary>
    /// Size of each tile in world units
    /// </summary>
    public Vector2 TileSize { get; set; } = new Vector2(1.0f, 1.0f);
    
    /// <summary>
    /// Path to the tileset texture asset
    /// </summary>
    public string TileSetPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of columns in the tileset texture
    /// </summary>
    public int TileSetColumns { get; set; } = 20;
    
    /// <summary>
    /// Number of rows in the tileset texture
    /// </summary>
    public int TileSetRows { get; set; } = 13;
    
    /// <summary>
    /// Layers of the tilemap
    /// </summary>
    public List<TileMapLayer> Layers { get; set; } = new();
    
    /// <summary>
    /// Currently active layer index for editing
    /// </summary>
    public int ActiveLayerIndex { get; set; } = 0;

    public TileMapComponent()
    {
        // Create default layer
        Layers.Add(new TileMapLayer(Width, Height) { Name = "Ground" });
    }

    /// <summary>
    /// Gets a tile value at the specified position and layer
    /// </summary>
    public int GetTile(int x, int y, int layer = 0)
    {
        if (layer < 0 || layer >= Layers.Count) return -1;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
        return Layers[layer].Tiles[x, y];
    }

    /// <summary>
    /// Sets a tile value at the specified position and layer
    /// </summary>
    public void SetTile(int x, int y, int tileIndex, int layer = 0)
    {
        if (layer < 0 || layer >= Layers.Count) return;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        Layers[layer].Tiles[x, y] = tileIndex;
    }

    /// <summary>
    /// Resizes the tilemap (preserves existing tile data where possible)
    /// </summary>
    public void Resize(int newWidth, int newHeight)
    {
        foreach (var layer in Layers)
        {
            var newTiles = new int[newWidth, newHeight];
            for (var x = 0; x < newWidth; x++)
            {
                for (var y = 0; y < newHeight; y++)
                {
                    if (x < Width && y < Height)
                    {
                        newTiles[x, y] = layer.Tiles[x, y];
                    }
                    else
                    {
                        newTiles[x, y] = -1;
                    }
                }
            }
            layer.Tiles = newTiles;
        }
        Width = newWidth;
        Height = newHeight;
    }

    /// <summary>
    /// Adds a new layer
    /// </summary>
    public void AddLayer(string name = "New Layer")
    {
        var layer = new TileMapLayer(Width, Height) { Name = name, ZIndex = Layers.Count };
        Layers.Add(layer);
    }

    /// <summary>
    /// Removes a layer at the specified index
    /// </summary>
    public void RemoveLayer(int index)
    {
        if (index >= 0 && index < Layers.Count && Layers.Count > 1)
        {
            Layers.RemoveAt(index);
            if (ActiveLayerIndex >= Layers.Count)
            {
                ActiveLayerIndex = Layers.Count - 1;
            }
        }
    }

    public IComponent Clone()
    {
        throw new NotImplementedException();
    }
}