using System.Numerics;

namespace Engine.Renderer.Textures;

public static class TilesetUv
{
    public static int Columns(int textureWidth, int tileSize, int margin, int spacing) =>
        StrideCount(textureWidth, tileSize, margin, spacing);

    public static int Rows(int textureHeight, int tileSize, int margin, int spacing) =>
        StrideCount(textureHeight, tileSize, margin, spacing);

    public static bool TryGetUvRect(
        int localIndex,
        int textureWidth,
        int textureHeight,
        int tileSize,
        int margin,
        int spacing,
        bool hFlip,
        bool vFlip,
        Vector2[] dest)
    {
        if (dest is not { Length: RenderingConstants.QuadVertexCount })
            return false;

        var columns = Columns(textureWidth, tileSize, margin, spacing);
        var rows = Rows(textureHeight, tileSize, margin, spacing);
        if (localIndex < 0 || columns < 1 || rows < 1 || localIndex >= columns * rows)
            return false;

        var tileX = localIndex % columns;
        var tileY = localIndex / columns;
        var tw = (float)textureWidth;
        var th = (float)textureHeight;

        var u0 = (margin + tileX * (tileSize + spacing)) / tw;
        var u1 = u0 + tileSize / tw;
        var v0 = 1f - (margin + (tileY + 1) * tileSize + tileY * spacing) / th;
        var v1 = 1f - (margin + tileY * (tileSize + spacing)) / th;

        if (hFlip)
            (u0, u1) = (u1, u0);
        if (vFlip)
            (v0, v1) = (v1, v0);

        dest[0] = new Vector2(u0, v0);
        dest[1] = new Vector2(u1, v0);
        dest[2] = new Vector2(u1, v1);
        dest[3] = new Vector2(u0, v1);
        return true;
    }

    private static int StrideCount(int axis, int tileSize, int margin, int spacing)
    {
        var stride = tileSize + spacing;
        if (tileSize < 1 || stride < 1 || axis <= 0)
            return 0;
        return (axis - 2 * margin + spacing) / stride;
    }
}
