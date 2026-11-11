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

            var texture = LoadOrCook(path, sRgb);
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
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        var bytesPerPixel = decoded.Data.Length / (decoded.Width * decoded.Height);
        var rgba = (decoded.DataFormat, bytesPerPixel) switch
        {
            (PixelFormat.Bgra, 4) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 4),
            (PixelFormat.Bgr, 3) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 3),
            _ => decoded.Data
        };
        return (rgba, decoded.Width, decoded.Height);
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

    private Texture2D LoadOrCook(string path, bool sRgb)
    {
        var normalizedPath = Path.GetFullPath(path);

        if (normalizedPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)
            && BcDds.TryRead(normalizedPath, out var sourceBc))
            return TryCreateCompressed(normalizedPath, sourceBc) ?? FallbackUncompressed(normalizedPath, sRgb);

        var sidecar = TextureSidecar.PathFor(normalizedPath);
        if (TextureSidecar.IsFresh(normalizedPath, sidecar) && BcDds.TryRead(sidecar, out var bc))
            return TryCreateCompressed(normalizedPath, bc) ?? FallbackUncompressed(normalizedPath, sRgb);

        if (_hostOptions.CookTextureSidecarOnLoad && TextureCookInference.IsCookableExtension(normalizedPath))
        {
            try
            {
                var rgba = BcTextureEncoder.DecodeToRgba(normalizedPath, sRgb);
                var decoded = TextureFileDecoder.Decode(normalizedPath, sRgb);
                var format = BcFormatPick.Pick(sRgb, TextureCookInference.IsNormalLoad(normalizedPath, sRgb), rgba,
                    decoded.Width, decoded.Height);
                if (CanUpload(format))
                {
                    var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, decoded.Width, decoded.Height, format);
                    if (image != null && dds != null && BcDds.Write(sidecar, image.Value, dds))
                        return OpenGLTexture2D.CreateCompressed(normalizedPath, image.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "BC cook failed for {Path}; falling back to RGBA", normalizedPath);
            }
        }

        if (TextureCookInference.IsCookableExtension(normalizedPath))
            Logger.Warning("BC sibling missing, stale, or unsupported for {Path}; uploading uncompressed", path);

        return FallbackUncompressed(normalizedPath, sRgb);
    }

    private static Texture2D? TryCreateCompressed(string path, BcImage bc)
    {
        if (!BcFormatPick.UsesS3Tc(bc.Format) || OpenGLTextureCaps.SupportsS3Tc)
        {
            try
            {
                return OpenGLTexture2D.CreateCompressed(path, bc);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Compressed upload failed for {Path}", path);
            }
        }
        else
        {
            Logger.Warning("S3TC unavailable; cannot upload {Format} for {Path}", bc.Format, path);
        }

        return null;
    }

    private static bool CanUpload(BcGpuFormat format) =>
        !BcFormatPick.UsesS3Tc(format) || OpenGLTextureCaps.SupportsS3Tc;

    private static Texture2D FallbackUncompressed(string path, bool sRgb) =>
        OpenGLTexture2D.Create(path, sRgb);

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
