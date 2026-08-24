using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
using SceneComponents.Rendering;

namespace Engine.Scene;

internal static class SubTextureRendererTexCoords
{
    public static Vector2[] Get(SubTextureRendererComponent component, Texture2D texture)
    {
        if (component is { TexCoordsManual: true, TexCoords: not null })
            return component.TexCoords;

        var key = HashKey(component, texture);
        if (component.TexCoords != null && component.TexCoordsCacheKey == key)
            return component.TexCoords;

        if (component.TexCoords is not { Length: RenderingConstants.QuadVertexCount })
            component.TexCoords = new Vector2[RenderingConstants.QuadVertexCount];

        SubTexture2D.FillTexCoordsFromCoords(
            texture, component.Coords, component.CellSize, component.SpriteSize, component.TexCoords);
        component.TexCoordsCacheKey = key;
        return component.TexCoords;
    }

    private static int HashKey(SubTextureRendererComponent component, Texture2D texture) =>
        HashCode.Combine(
            component.Coords.X, component.Coords.Y,
            component.CellSize.X, component.CellSize.Y,
            component.SpriteSize.X, component.SpriteSize.Y,
            texture.Width, texture.Height);
}
