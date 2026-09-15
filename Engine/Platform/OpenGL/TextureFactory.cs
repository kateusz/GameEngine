using Engine.Renderer.Textures;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace Engine.Platform.OpenGL;

internal sealed class TextureFactory : ITextureFactory
{
    private const int PreviewMaxEdge = 64;
    private Texture2D? _whiteTexture;
    private Texture2D? _blackTexture;
    private Texture2D? _flatNormalTexture;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public Texture2D GetWhiteTexture() => GetOrCreateSolid(ref _whiteTexture, 0xFFFFFFFF);
    public Texture2D GetBlackTexture() => GetOrCreateSolid(ref _blackTexture, 0xFF000000u);
    public Texture2D GetFlatNormalTexture() => GetOrCreateSolid(ref _flatNormalTexture, 0xFFFF8080u);

    public Texture2D Create(string path, bool sRgb = false)
    {
        var normalizedPath = Path.GetFullPath(path);
        var cacheKey = sRgb ? normalizedPath + "#srgb" : normalizedPath;

        lock (_cacheLock)
        {
            if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
                return cachedTexture;

            var texture = OpenGLTexture2D.Create(path, sRgb);
            _textureCache[cacheKey] = texture;
            return texture;
        }
    }

    public (byte[] Data, int Width, int Height) DecodePreview(string path)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb: true);
        var bytesPerPixel = decoded.Data.Length / (decoded.Width * decoded.Height);
        var rgba = (decoded.DataFormat, bytesPerPixel) switch
        {
            (PixelFormat.Bgra, 4) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 4),
            (PixelFormat.Bgr, 3) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 3),
            _ => decoded.Data
        };
        return TexturePreviewScaling.DownscaleRgba(rgba, decoded.Width, decoded.Height, PreviewMaxEdge);
    }

    public Texture2D CreateFromRgba(byte[] rgba, int width, int height) =>
        OpenGLTexture2D.CreateFromRgba(rgba, width, height);

    public Texture2D Create(int width, int height) => OpenGLTexture2D.Create(width, height);

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_cacheLock)
        {
            _whiteTexture?.Dispose();
            _whiteTexture = null;
            _blackTexture?.Dispose();
            _blackTexture = null;
            _flatNormalTexture?.Dispose();
            _flatNormalTexture = null;

            foreach (var texture in _textureCache.Values)
                texture.Dispose();
            _textureCache.Clear();
        }

        _disposed = true;
    }

    private Texture2D GetOrCreateSolid(ref Texture2D? field, uint rgba)
    {
        if (field != null)
            return field;

        lock (_cacheLock)
        {
            if (field != null)
                return field;

            field = Create(1, 1);
            field.SetData(rgba, 4);
            return field;
        }
    }
}
