using SceneComponents.Rendering;

namespace Editor.Features.Tiled;

public sealed class TiledMapData
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int TileSize { get; init; }
    public List<TileMapLayer> Layers { get; init; } = [];
    public List<TiledObjectData> Objects { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}