using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public sealed class TileMapLayer
{
    public string Name { get; set; } = "";
    public bool Visible { get; set; } = true;
    public string? TexturePath { get; set; }
    public int TileSize { get; set; }
    public int Margin { get; set; }
    public int Spacing { get; set; }
    public int[] Tiles { get; set; } = [];
    public byte[] Flags { get; set; } = [];

    public void SetTile(int mapWidth, int mapHeight, int x, int y, int tileIndex, byte flags = 0)
    {
        if ((uint)x >= (uint)mapWidth || (uint)y >= (uint)mapHeight)
            return;
        if (tileIndex < -1)
            return;

        Repair(mapWidth, mapHeight);
        var i = y * mapWidth + x;
        Tiles[i] = tileIndex;
        Flags[i] = tileIndex < 0 ? (byte)0 : flags;
    }

    public void Repair(int mapWidth, int mapHeight)
    {
        var len = System.Math.Max(0, mapWidth) * System.Math.Max(0, mapHeight);
        Tiles = Fit(Tiles, len, -1);
        Flags = Fit(Flags, len, (byte)0);
    }

    public TileMapLayer Clone() => new()
    {
        Name = Name,
        Visible = Visible,
        TexturePath = TexturePath,
        TileSize = TileSize,
        Margin = Margin,
        Spacing = Spacing,
        Tiles = (int[])Tiles.Clone(),
        Flags = (byte[])Flags.Clone()
    };

    private static T[] Fit<T>(T[] source, int length, T fill)
    {
        if (source.Length == length)
            return source;
        var next = new T[length];
        Array.Fill(next, fill);
        if (source.Length > 0 && length > 0)
            Array.Copy(source, next, System.Math.Min(source.Length, length));
        return next;
    }
}

public sealed class TileMapComponent : IComponent, IJsonOnDeserialized
{
    public const byte FlipH = 1;
    public const byte FlipV = 2;

    public string? SourceMapPath { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int TileSize { get; set; } = 16;

    public List<TileMapLayer> Layers { get; set; } = [];

    public void OnDeserialized() => Repair();

    public void Repair()
    {
        foreach (var layer in Layers)
            layer.Repair(Width, Height);
    }

    public void CopyFrom(TileMapComponent other)
    {
        SourceMapPath = other.SourceMapPath;
        Width = other.Width;
        Height = other.Height;
        TileSize = other.TileSize;
        Layers = other.Layers.Select(l => l.Clone()).ToList();
    }

    public IComponent Clone()
    {
        return new TileMapComponent
        {
            SourceMapPath = SourceMapPath,
            Width = Width,
            Height = Height,
            TileSize = TileSize,
            Layers = Layers.Select(l => l.Clone()).ToList()
        };
    }
}
