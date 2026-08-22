using Engine.Platform.OpenGL;

namespace Engine.Renderer.Shaders;

/// <summary>
/// Factory for creating and managing shader resources with automatic caching.
/// Uses weak references to allow garbage collection when shaders are no longer in use.
/// </summary>
internal sealed class ShaderFactory(IRendererApiConfig apiConfig) : IShaderFactory, IDisposable
{
    private readonly Dictionary<(string Vert, string Frag, DateTime, DateTime), WeakReference<IShader>> _shaderCache = new();
    private readonly Lock _cacheLock = new();
    private bool _disposed;
    
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
            vertModTime = DateTime.MinValue;
            fragModTime = DateTime.MinValue;
        }

        var key = (vertPath, fragPath, vertModTime, fragModTime);

        lock (_cacheLock)
        {
            if (_shaderCache.TryGetValue(key, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedShader))
                    return cachedShader;

                _shaderCache.Remove(key);
            }
        }

        var shader = apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLShader(vertPath, fragPath),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };

        lock (_cacheLock)
        {
            if (_shaderCache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var cachedShader))
            {
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
            foreach (var weakRef in _shaderCache.Values)
            {
                if (weakRef.TryGetTarget(out var shader))
                {
                    shader?.Dispose();
                }
            }

            _shaderCache.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearCache();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    ~ShaderFactory()
    {
        if (!_disposed)
        {
            System.Diagnostics.Debug.WriteLine(
                "FACTORY LEAK: ShaderFactory not disposed!"
            );
        }
    }
#endif
}
