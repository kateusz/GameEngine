using Engine.Platform.SilkNet;

namespace Engine.Renderer.Textures;

/// <summary>
/// Factory for creating and managing texture resources with automatic caching.
/// Uses weak references to allow garbage collection of unused textures while preventing duplicate GPU allocations.
/// </summary>
public static class TextureFactory
{
    private static Texture2D? _whiteTexture;
    private static readonly object _whiteLock = new();
    private static readonly Dictionary<string, WeakReference<Texture2D>> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _cacheLock = new();

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

    /// <summary>
    /// Creates or retrieves a cached texture from the specified file path.
    /// This method uses weak-reference-based caching to prevent duplicate GPU resource allocations
    /// while allowing the garbage collector to reclaim unused textures.
    /// </summary>
    /// <param name="path">The file path to the texture resource.</param>
    /// <returns>A texture instance, either from cache or newly created.</returns>
    /// <remarks>
    /// Thread-safe: Multiple threads can safely call this method concurrently.
    /// Performance: Cache hits avoid redundant texture loading and GPU memory allocation.
    /// Memory: Weak references allow GC to reclaim textures when no strong references exist.
    /// </remarks>
    public static Texture2D Create(string path)
    {
        // Normalize the path to ensure cache consistency across different path representations
        string normalizedPath = Path.GetFullPath(path);

        lock (_cacheLock)
        {
            // Check cache first
            if (_textureCache.TryGetValue(normalizedPath, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedTexture))
                {
                    return cachedTexture;
                }
                else
                {
                    // Weak reference died, remove from cache
                    _textureCache.Remove(normalizedPath);
                }
            }

            // Create new texture (use original path for loading)
            var texture = RendererApiType.Type switch
            {
                ApiType.SilkNet => SilkNetTexture2D.Create(path),
                _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
            };

            // Add to cache with weak reference using normalized path
            _textureCache[normalizedPath] = new WeakReference<Texture2D>(texture);
            return texture;
        }
    }

    /// <summary>
    /// Creates a new procedural texture with the specified dimensions.
    /// Procedural textures are not cached as they may have different content despite identical dimensions.
    /// </summary>
    /// <param name="width">The width of the texture in pixels.</param>
    /// <param name="height">The height of the texture in pixels.</param>
    /// <returns>A new texture instance with the specified dimensions.</returns>
    /// <remarks>
    /// This overload does not use caching because procedural textures created with the same dimensions
    /// may contain different pixel data and should not be deduplicated.
    /// </remarks>
    public static Texture2D Create(int width, int height)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => SilkNetTexture2D.Create(width, height),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }

    /// <summary>
    /// Clears all cached textures, removing weak references from the cache.
    /// Useful during scene transitions or when explicit memory management is required.
    /// </summary>
    /// <remarks>
    /// This method does not dispose of texture resources. Textures will be disposed when their
    /// strong references are released and the garbage collector finalizes them.
    /// Thread-safe: Can be called from any thread.
    /// </remarks>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _textureCache.Clear();
        }
    }

    /// <summary>
    /// Gets the current number of entries in the texture cache.
    /// Note that this includes both live textures and dead weak references that haven't been cleaned up yet.
    /// </summary>
    /// <returns>The number of cache entries.</returns>
    /// <remarks>
    /// This method is primarily useful for debugging and diagnostics.
    /// The count may include dead weak references until they are accessed and cleaned up.
    /// Thread-safe: Can be called from any thread.
    /// </remarks>
    public static int GetCacheSize()
    {
        lock (_cacheLock)
        {
            return _textureCache.Count;
        }
    }
}