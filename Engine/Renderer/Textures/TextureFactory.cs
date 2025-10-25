using Engine.Platform.SilkNet;

namespace Engine.Renderer.Textures;

public static class TextureFactory
{
    private static Texture2D? _whiteTexture;
    private static readonly object _whiteLock = new();

    /// <summary>
    /// Gets a shared singleton 1x1 white texture.
    /// This method is thread-safe and ensures only one white texture is created for the entire application.
    /// </summary>
    /// <returns>A shared white texture instance.</returns>
    public static Texture2D GetWhiteTexture()
    {
        if (_whiteTexture != null)
            return _whiteTexture;

        lock (_whiteLock)
        {
            // Double-check pattern to avoid race conditions
            if (_whiteTexture != null)
                return _whiteTexture;

            _whiteTexture = Create(1, 1);

            // Set white pixel data (0xFFFFFFFF = white in RGBA format)
            unsafe
            {
                uint white = 0xFFFFFFFF;
                _whiteTexture.SetData(white, 4);
            }

            return _whiteTexture;
        }
    }

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