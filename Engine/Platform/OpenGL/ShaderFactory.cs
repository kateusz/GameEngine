using Engine.Renderer.Shaders;

namespace Engine.Platform.OpenGL;

/// <summary>
/// Factory for creating and managing shader resources with automatic caching.
/// </summary>
internal sealed class ShaderFactory : IShaderFactory
{
    private readonly Dictionary<(string Vert, string Frag), IShader> _shaderCache = new();
    private readonly Lock _cacheLock = new();
    private bool _disposed;

    public IShader Create(string vertPath, string fragPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = (vertPath, fragPath);

        lock (_cacheLock)
        {
            if (_shaderCache.TryGetValue(key, out var cachedShader))
                return cachedShader;
        }

        var shader = new OpenGLShader(vertPath, fragPath);

        lock (_cacheLock)
        {
            if (_shaderCache.TryGetValue(key, out var cachedShader))
            {
                shader.Dispose();
                return cachedShader;
            }

            _shaderCache[key] = shader;
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
            foreach (var shader in _shaderCache.Values)
                shader.Dispose();

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
