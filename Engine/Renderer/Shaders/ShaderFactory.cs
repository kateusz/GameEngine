using Engine.Platform.SilkNet;

namespace Engine.Renderer.Shaders;

public static class ShaderFactory
{
    private static readonly Dictionary<(string, string, DateTime, DateTime), WeakReference<IShader>> _shaderCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Creates or retrieves a cached shader instance for the specified vertex and fragment shader paths.
    /// Uses weak references to allow garbage collection when shaders are no longer in use.
    /// </summary>
    /// <param name="vertPath">Path to the vertex shader file.</param>
    /// <param name="fragPath">Path to the fragment shader file.</param>
    /// <returns>A shader instance, either from cache or newly created.</returns>
    /// <remarks>
    /// This method is thread-safe. Shader compilation is expensive (100ms+), so caching
    /// significantly improves performance when the same shader is requested multiple times.
    /// The cache automatically invalidates when shader files are modified on disk.
    /// </remarks>
    public static IShader Create(string vertPath, string fragPath)
    {
        // Get file modification times for cache invalidation
        DateTime vertModTime, fragModTime;
        try
        {
            vertModTime = File.GetLastWriteTimeUtc(vertPath);
            fragModTime = File.GetLastWriteTimeUtc(fragPath);
        }
        catch (Exception)
        {
            // If files don't exist or can't be accessed, use DateTime.MinValue
            // This will force shader creation which will fail appropriately
            vertModTime = DateTime.MinValue;
            fragModTime = DateTime.MinValue;
        }

        var key = (vertPath, fragPath, vertModTime, fragModTime);

        // First check: Look for cached shader
        lock (_cacheLock)
        {
            if (_shaderCache.TryGetValue(key, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedShader))
                {
                    return cachedShader;
                }
                else
                {
                    // Weak reference target was collected, remove dead entry
                    _shaderCache.Remove(key);
                }
            }
        }

        // Create shader outside of lock to allow concurrent creation of different shaders
        var shader = RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetShader(vertPath, fragPath),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };

        // Second check: Store in cache (double-checked locking pattern)
        lock (_cacheLock)
        {
            // Another thread may have created and cached this shader while we were compiling
            if (_shaderCache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var cachedShader))
            {
                // Another thread won the race; dispose our shader and return the cached one
                shader.Dispose();
                return cachedShader;
            }

            _shaderCache[key] = new WeakReference<IShader>(shader);
            return shader;
        }
    }

    /// <summary>
    /// Clears the shader cache, forcing all subsequent shader requests to recompile.
    /// Useful for development scenarios where shaders need to be reloaded.
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _shaderCache.Clear();
        }
    }
}