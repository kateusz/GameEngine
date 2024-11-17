using Engine.Platform.SilkNet;

namespace Engine.Renderer.Textures;

public static class TextureFactory
{
    public static Texture2D Create(string path)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => SilkNetTexture2D.Create(path),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }

    public static Texture2D Create(int width, int height)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => SilkNetTexture2D.Create(width, height),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}