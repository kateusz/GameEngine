using Engine.Renderer.Textures;

namespace Engine.Tiles;

/// <summary>
/// Represents a single tile definition in a tileset
/// </summary>
public record Tile
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required SubTexture2D? SubTexture { get; init; }
}