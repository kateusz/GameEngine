using Engine.Renderer.Shaders;

namespace Engine.Platform.OpenGL;

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

            var shader = new OpenGLShader(vertPath, fragPath);
            _shaderCache[key] = shader;
            return shader;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_cacheLock)
        {
            foreach (var shader in _shaderCache.Values)
                shader.Dispose();
            _shaderCache.Clear();
        }

        _disposed = true;
    }
}
