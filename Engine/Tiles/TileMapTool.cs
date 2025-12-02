namespace Engine.Tiles;

/// <summary>
/// Available tools for tilemap editing.
/// </summary>
public enum TileMapTool
{
    /// <summary>
    /// Paint tiles onto the tilemap.
    /// </summary>
    Paint,

    /// <summary>
    /// Erase tiles from the tilemap.
    /// </summary>
    Erase,

    /// <summary>
    /// Flood fill an area with the selected tile.
    /// </summary>
    Fill,

    /// <summary>
    /// Select tiles on the tilemap.
    /// </summary>
    Select
}
