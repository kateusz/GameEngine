using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures;

internal sealed class TextureFactory(IRendererApiConfig apiConfig) : ITextureFactory, IDisposable
{
    private Texture2D? _whiteTexture;
    private readonly Lock _whiteLock = new();
    private Texture2D? _blackTexture;
    private readonly Lock _blackLock = new();
    private Texture2D? _flatNormalTexture;
    private readonly Lock _flatNormalLock = new();
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
            var white = 0xFFFFFFFF;
            _whiteTexture.SetData(white, 4);

            return _whiteTexture;
        }
    }
    
    public Texture2D GetBlackTexture()
    {
        if (_blackTexture != null)
            return _blackTexture;

        lock (_blackLock)
        {
            if (_blackTexture != null)
                return _blackTexture;

            _blackTexture = Create(1, 1);
            _blackTexture.SetData(0xFF000000u, 4);
            return _blackTexture;
        }
    }

    public Texture2D GetFlatNormalTexture()
    {
        if (_flatNormalTexture != null)
            return _flatNormalTexture;

        lock (_flatNormalLock)
        {
            if (_flatNormalTexture != null)
                return _flatNormalTexture;

            _flatNormalTexture = Create(1, 1);
            _flatNormalTexture.SetData(0xFFFF8080u, 4);
            return _flatNormalTexture;
        }
    }

    public Texture2D Create(string path)
    {
        // Normalize the path to ensure cache consistency across different path representations
        var normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            // Check cache first
            if (_textureCache.TryGetValue(normalizedPath, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedTexture))
                {
                    return cachedTexture;
                }

                // Weak reference died, remove from cache
                _textureCache.Remove(normalizedPath);
            }

            // Create new texture (use original path for loading)
            var texture = apiConfig.Type switch
            {
                ApiType.SilkNet => OpenGLTexture2D.Create(path),
                _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
            };

            // Add to cache with weak reference using normalized path
            _textureCache[normalizedPath] = new WeakReference<Texture2D>(texture);
            return texture;
        }
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

        lock (_blackLock)
        {
            _blackTexture?.Dispose();
            _blackTexture = null;
        }

        lock (_flatNormalLock)
        {
            _flatNormalTexture?.Dispose();
            _flatNormalTexture = null;
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
