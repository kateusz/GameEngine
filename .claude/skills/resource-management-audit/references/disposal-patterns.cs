// Resource Management Disposal Patterns
// Reference examples for proper IDisposable implementation in game engine

using System;
using OpenGL; // Placeholder - actual project uses Silk.NET.OpenGL

// ============================================================================
// PATTERN 1: Basic Disposal (No Inheritance)
// ============================================================================
// Use when:
// - Class is sealed or not intended for inheritance
// - Only unmanaged resources (OpenGL objects)
// - No derived classes need custom disposal logic

public class Texture : IDisposable
{
    private uint _rendererID;
    private bool _disposed = false;

    public Texture(string path)
    {
        // Load texture and generate OpenGL ID
        _rendererID = GL.GenTexture();
        // ... texture loading code
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;  // Prevent double-deletion
        }

        _disposed = true;
        GC.SuppressFinalize(this);  // Prevent finalizer from running
    }
}

// ============================================================================
// PATTERN 2: Full Disposal Pattern (Supports Inheritance)
// ============================================================================
// Use when:
// - Class may be inherited
// - Mix of managed and unmanaged resources
// - Derived classes need to add disposal logic

public class Mesh : IDisposable
{
    private uint _vao, _vbo, _ebo;
    private bool _disposed = false;

    public Mesh(float[] vertices, uint[] indices)
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        // ... mesh setup code
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources here
            // (none in this case, but derived classes might have them)
        }

        // Dispose unmanaged resources (OpenGL objects)
        if (_vao != 0)
        {
            GL.DeleteVertexArray(_vao);
            _vao = 0;
        }

        if (_vbo != 0)
        {
            GL.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        if (_ebo != 0)
        {
            GL.DeleteBuffer(_ebo);
            _ebo = 0;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Mesh()
    {
        // WARNING: Finalizers should NOT call OpenGL!
        // OpenGL context may not be available on finalizer thread
        // Log warning instead
        if (!_disposed && _vao != 0)
        {
            Logger.Warning("Mesh was not properly disposed!");
        }
    }
}

// ============================================================================
// PATTERN 3: Factory-Managed Resources
// ============================================================================
// Use when:
// - Resources are cached and shared
// - Factory owns lifetime management
// - Consumers don't dispose individual resources

public class TextureFactory : IDisposable
{
    private Dictionary<string, Texture> _cache = new();
    private bool _disposed = false;

    public Texture Load(string path)
    {
        if (_cache.TryGetValue(path, out var texture))
            return texture;

        texture = new Texture(path);
        _cache[path] = texture;
        return texture;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Factory is responsible for disposing all cached textures
        foreach (var texture in _cache.Values)
        {
            texture.Dispose();
        }
        _cache.Clear();

        _disposed = true;
    }
}

// ============================================================================
// PATTERN 4: Shader Programs (Multiple OpenGL Objects)
// ============================================================================

public class ShaderProgram : IDisposable
{
    private uint _programID;
    private uint _vertexShaderID;
    private uint _fragmentShaderID;
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        // Delete shader program first
        if (_programID != 0)
        {
            GL.DeleteProgram(_programID);
            _programID = 0;
        }

        // Then delete individual shaders
        if (_vertexShaderID != 0)
        {
            GL.DeleteShader(_vertexShaderID);
            _vertexShaderID = 0;
        }

        if (_fragmentShaderID != 0)
        {
            GL.DeleteShader(_fragmentShaderID);
            _fragmentShaderID = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// ============================================================================
// PATTERN 5: Framebuffer (Multiple Resource Types)
// ============================================================================

public class Framebuffer : IDisposable
{
    private uint _fboID;
    private uint _colorTextureID;
    private uint _depthRboID;
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        // Delete in reverse order of creation
        if (_depthRboID != 0)
        {
            GL.DeleteRenderbuffer(_depthRboID);
            _depthRboID = 0;
        }

        if (_colorTextureID != 0)
        {
            GL.DeleteTexture(_colorTextureID);
            _colorTextureID = 0;
        }

        if (_fboID != 0)
        {
            GL.DeleteFramebuffer(_fboID);
            _fboID = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
