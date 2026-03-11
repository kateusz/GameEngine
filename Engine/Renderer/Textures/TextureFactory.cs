using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures;

internal sealed class TextureFactory(IRendererApiConfig apiConfig) : ITextureFactory, IDisposable
{
    private Texture2D? _whiteTexture;
    private readonly Lock _whiteLock = new();
    private readonly Dictionary<string, WeakReference<Texture2D>> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public Texture2D GetWhiteTexture()
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
                var white = 0xFFFFFFFF;
                _whiteTexture.SetData(white, 4);
            }

            return _whiteTexture;
        }
    }
    
    public Texture2D Create(string path) => Create(path, srgb: false);

    public Texture2D Create(string path, bool srgb)
    {
        // Include srgb flag in cache key so the same file can be cached in both formats
        var normalizedPath = Path.GetFullPath(path);
        var cacheKey = srgb ? normalizedPath + "|srgb" : normalizedPath;

        lock (_cacheLock)
        {
            if (_textureCache.TryGetValue(cacheKey, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedTexture))
                {
                    return cachedTexture;
                }

                _textureCache.Remove(cacheKey);
            }

            var texture = apiConfig.Type switch
            {
                ApiType.SilkNet => OpenGLTexture2D.Create(normalizedPath, srgb),
                _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
            };

            _textureCache[cacheKey] = new WeakReference<Texture2D>(texture);
            return texture;
        }
    }

    public bool IsSupportedFormat(string path)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => OpenGLTexture2D.IsSupportedFormat(path),
            _ => false
        };
    }
    
    public Texture2D Create(int width, int height)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => OpenGLTexture2D.Create(width, height),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
    
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            foreach (var weakRef in _textureCache.Values)
            {
                if (weakRef.TryGetTarget(out var texture))
                {
                    texture?.Dispose();
                }
            }

            _textureCache.Clear();
        }
    }

    public int GetCacheSize()
    {
        lock (_cacheLock)
        {
            return _textureCache.Count;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_whiteLock)
        {
            _whiteTexture?.Dispose();
            _whiteTexture = null;
        }

        ClearCache();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~TextureFactory()
    {
        if (!_disposed)
        {
            System.Diagnostics.Debug.WriteLine(
                $"FACTORY LEAK: TextureFactory not disposed! " +
                $"White texture: {(_whiteTexture != null ? "allocated" : "null")}"
            );
        }
    }
#endif
}
