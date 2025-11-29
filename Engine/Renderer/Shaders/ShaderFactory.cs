using Engine.Platform.SilkNet;

namespace Engine.Renderer.Shaders;

/// <summary>
/// Factory for creating and managing shader resources with automatic caching.
/// Uses weak references to allow garbage collection when shaders are no longer in use.
/// </summary>
internal sealed class ShaderFactory(IRendererApiConfig apiConfig) : IShaderFactory
{
    private readonly Dictionary<(string, string, DateTime, DateTime), WeakReference<IShader>> _shaderCache = new();
    private readonly Lock _cacheLock = new();

    public IShader Create(string vertPath, string fragPath)
    {
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

                // Weak reference target was collected, remove dead entry
                _shaderCache.Remove(key);
            }
        }

        // Create shader outside of lock to allow concurrent creation of different shaders
        var shader = apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetShader(vertPath, fragPath),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
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
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _shaderCache.Clear();
        }
    }
}
