using Engine.Core.DI;
using Engine.Renderer.Textures;
using Engine.Renderer.Textures.Compression;
using Serilog;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace Engine.Platform.OpenGL;

internal sealed class TextureFactory : ITextureFactory
{
    private static readonly ILogger Logger = Log.ForContext<TextureFactory>();

    private const int PreviewMaxEdge = 64;
    private Texture2D? _whiteTexture;
    private Texture2D? _blackTexture;
    private Texture2D? _flatNormalTexture;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly EngineHostOptions _hostOptions;
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public TextureFactory(EngineHostOptions hostOptions) => _hostOptions = hostOptions;

    public Texture2D GetWhiteTexture() => GetOrCreateSolid(ref _whiteTexture, 0xFFFFFFFF);
    public Texture2D GetBlackTexture() => GetOrCreateSolid(ref _blackTexture, 0xFF000000u);
    public Texture2D GetFlatNormalTexture() => GetOrCreateSolid(ref _flatNormalTexture, 0xFFFF8080u);

    public Texture2D Create(string path, bool sRgb = false)
    {
        var cacheKey = ToCacheKey(path, sRgb);

        lock (_cacheLock)
        {
            if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
                return cachedTexture;

            var texture = LoadWithDdsSibling(path, sRgb);
            _textureCache[cacheKey] = texture;
            return texture;
        }
    }

    public (byte[] Data, int Width, int Height) DecodePreview(string path)
    {
        var (rgba, width, height) = Decode(path, sRgb: true);
        return TexturePreviewScaling.DownscaleRgba(rgba, width, height, PreviewMaxEdge);
    }

    public (byte[] Data, int Width, int Height) Decode(string path, bool sRgb)
    {
        var (rgba, width, height) = TextureRasterToRgba8.Decode(path, sRgb);
        return (rgba, width, height);
    }

    public Texture2D CreateFromRgba(byte[] rgba, int width, int height, bool sRgb = false, string? cachePath = null)
    {
        if (cachePath == null)
            return OpenGLTexture2D.CreateFromRgba(rgba, width, height);

        var cacheKey = ToCacheKey(cachePath, sRgb);

        lock (_cacheLock)
        {
            if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
                return cachedTexture;

            var normalizedPath = Path.GetFullPath(cachePath);
            var texture = OpenGLTexture2D.CreateFromRgba(rgba, width, height, sRgb, generateMipmaps: true,
                normalizedPath);
            _textureCache[cacheKey] = texture;
            return texture;
        }
    }

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

    private Texture2D LoadWithDdsSibling(string path, bool sRgb)
    {
        var normalizedPath = Path.GetFullPath(path);

        if (normalizedPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)
            && BcDds.TryRead(normalizedPath, out var sourceBc)
            && SidecarMatchesLoad(sourceBc, sRgb, TextureBcSource.IsNormalLoad(normalizedPath, sRgb)))
            return TryCreateCompressed(normalizedPath, sourceBc) ?? OpenGLTexture2D.Create(normalizedPath, sRgb);

        if (!TextureBcSource.IsRasterSourceExtension(normalizedPath))
            return OpenGLTexture2D.Create(normalizedPath, sRgb);

        var isNormal = TextureBcSource.IsNormalLoad(normalizedPath, sRgb);
        var isOcclusion = TextureBcSource.IsOcclusionMap(normalizedPath);
        var sidecar = BcDdsSidecarPath.ForSource(normalizedPath, sRgb);

        if (TextureBcSidecar.SidecarUsable(normalizedPath, sidecar, sRgb, isNormal)
            && BcDds.TryRead(sidecar, out var bc))
            return TryCreateCompressed(normalizedPath, bc) ?? OpenGLTexture2D.Create(normalizedPath, sRgb);

        if (_hostOptions.EnsureTextureDdsSiblingOnLoad)
        {
            try
            {
                var (rgba, width, height) = BcTextureEncoder.DecodeToRgba(normalizedPath, sRgb);
                var format = BcFormatPick.Pick(sRgb, isNormal, isOcclusion, rgba, width, height);
                var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, width, height, format, sRgb, isNormal);
                if (image != null && dds != null && BcDds.Write(sidecar, image.Value, dds))
                    return OpenGLTexture2D.CreateCompressed(normalizedPath, image.Value);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "BC encode failed for {Path}; falling back to RGBA", normalizedPath);
            }
        }

        Logger.Warning("BC sibling missing, stale, or unsupported for {Path}; uploading uncompressed", path);
        return OpenGLTexture2D.Create(normalizedPath, sRgb);
    }

    private static bool SidecarMatchesLoad(BcImage bc, bool sRgb, bool isNormal) =>
        bc.SRgb == sRgb && bc.IsNormal == isNormal;

    private static Texture2D? TryCreateCompressed(string path, BcImage bc)
    {
        try
        {
            return OpenGLTexture2D.CreateCompressed(path, bc);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Compressed upload failed for {Path}", path);
            return null;
        }
    }

    private static string ToCacheKey(string path, bool sRgb)
    {
        var normalizedPath = Path.GetFullPath(path);
        return sRgb ? normalizedPath + "#srgb" : normalizedPath;
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
