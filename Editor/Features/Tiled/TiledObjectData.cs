using System.Numerics;

namespace Editor.Features.Tiled;

public sealed class TiledObjectData
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public Dictionary<string, string> Properties { get; init; } = new(StringComparer.Ordinal);
    public Vector3 LocalCenter { get; init; }
    public Vector3 Rotation { get; init; }
    public Vector3 Scale { get; init; } = Vector3.One;
    public Vector2? BoxHalfExtents { get; init; }
    public bool IsTrigger { get; init; }
    public string? SubTexturePath { get; init; }
    public Vector2 SubTextureCoords { get; init; }
    public Vector2 SubTextureCellSize { get; init; }
}