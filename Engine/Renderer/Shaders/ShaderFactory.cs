using Engine.Platform.SilkNet;

namespace Engine.Renderer.Shaders;

public static class ShaderFactory
{
    private static readonly Dictionary<(string, string), WeakReference<IShader>> _shaderCache = new();
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
    /// </remarks>
    public static IShader Create(string vertPath, string fragPath)
    {
        var key = (vertPath, fragPath);

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

            var shader = RendererApiType.Type switch
            {
                ApiType.SilkNet => new SilkNetShader(vertPath, fragPath),
                _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
            };

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