---
name: resource-management-audit
description: Audit resource management including IDisposable pattern implementation, proper cleanup of OpenGL resources (buffers, textures, shaders, framebuffers), memory leak detection, resource lifetime management, and GPU resource tracking. Use when investigating memory leaks, GPU resource exhaustion, or implementing new resource types.
---

# Resource Management Audit

## Overview
This skill audits resource management patterns to ensure proper cleanup of unmanaged resources (OpenGL objects, file handles, native memory), correct IDisposable implementation, and prevention of memory leaks.

## When to Use
Invoke this skill when:
- Investigating memory leaks or growing memory usage
- GPU resources (textures, buffers) are not being cleaned up
- Adding new resource types (textures, shaders, meshes)
- Refactoring resource lifetime management
- Application crashes on shutdown (disposal issues)
- Out-of-memory errors or GPU resource exhaustion
- Questions about proper disposal patterns

## Resource Types in Engine

### OpenGL Resources (GPU)
1. **Textures**: `glGenTextures`, `glDeleteTextures`
2. **Buffers**: `glGenBuffers`, `glDeleteBuffers` (VBO, EBO, UBO)
3. **Vertex Arrays**: `glGenVertexArrays`, `glDeleteVertexArrays` (VAO)
4. **Framebuffers**: `glGenFramebuffers`, `glDeleteFramebuffers`
5. **Shaders**: `glCreateShader`, `glDeleteShader`
6. **Programs**: `glCreateProgram`, `glDeleteProgram`

### Managed Resources (CPU)
1. **File Handles**: `FileStream`, `StreamReader`
2. **Audio Buffers**: OpenAL buffers and sources
3. **Native Memory**: Pinned arrays, unmanaged allocations

## Proper IDisposable Pattern

### Basic Pattern (No Derived Classes)
```csharp
public class Texture : IDisposable
{
    private uint _rendererID;
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

### Full Pattern (Supports Derivation)
```csharp
public class Mesh : IDisposable
{
    private uint _vao, _vbo, _ebo;
    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources here
            // (none in this case)
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
```

## Common Disposal Anti-Patterns

### 1. Missing Disposal
```csharp
// ❌ WRONG - Texture never disposed
public void LoadTexture(string path)
{
    var texture = new Texture(path);
    // texture goes out of scope without being disposed
    // GPU memory leak!
}

// ✅ CORRECT - Use using statement
public Texture LoadTexture(string path)
{
    // Caller is responsible for disposal
    return new Texture(path);
}

// Or cache in factory with disposal
public class TextureFactory : IDisposable
{
    private Dictionary<string, Texture> _cache = new();

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
        foreach (var texture in _cache.Values)
        {
            texture.Dispose();
        }
        _cache.Clear();
    }
}
```

### 2. Double Disposal
```csharp
// ❌ WRONG - Can crash if disposed twice
public void Dispose()
{
    GL.DeleteTexture(_rendererID); // Crashes if called twice!
}

// ✅ CORRECT - Guard against double disposal
public void Dispose()
{
    if (_disposed)
        return;

    if (_rendererID != 0)
    {
        GL.DeleteTexture(_rendererID);
        _rendererID = 0; // Reset to prevent double-delete
    }

    _disposed = true;
}
```

### 3. Disposing Shared Resources
```csharp
// ❌ WRONG - Multiple objects share same GPU buffer
public class ModelInstance : IDisposable
{
    private Mesh _sharedMesh; // Shared!

    public void Dispose()
    {
        _sharedMesh.Dispose(); // WRONG - other instances still using it!
    }
}

// ✅ CORRECT - Don't dispose shared resources
public class ModelInstance : IDisposable
{
    private Mesh _sharedMesh; // Reference, not owned

    public void Dispose()
    {
        // Don't dispose _sharedMesh - it's managed by factory/pool
        // Only dispose resources THIS instance owns
    }
}
```

### 4. OpenGL Calls in Finalizers
```csharp
// ❌ WRONG - OpenGL not safe in finalizer
public class Texture : IDisposable
{
    ~Texture()
    {
        GL.DeleteTexture(_rendererID); // CRASH! No GL context on finalizer thread
    }
}

// ✅ CORRECT - Log warning instead
public class Texture : IDisposable
{
    ~Texture()
    {
        if (_rendererID != 0)
        {
            Logger.Error($"Texture {_path} was not properly disposed!");
            // Don't call GL functions!
        }
    }

    public void Dispose()
    {
        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;
        }
        GC.SuppressFinalize(this); // Prevent finalizer from running
    }
}
```

## Resource Ownership Patterns

### Factory-Owned Resources
```csharp
public interface ITextureFactory : IDisposable
{
    Texture LoadTexture(string path);
}

public class TextureFactory : ITextureFactory
{
    private readonly Dictionary<string, Texture> _cache = new();

    public Texture LoadTexture(string path)
    {
        // Factory owns the texture lifetime
        if (_cache.TryGetValue(path, out var texture))
            return texture;

        texture = new Texture(path);
        _cache[path] = texture;
        return texture;
    }

    public void Dispose()
    {
        // Factory disposes all owned resources
        foreach (var texture in _cache.Values)
        {
            texture.Dispose();
        }
        _cache.Clear();
    }
}

// Usage - DO NOT dispose textures from factory
public class Renderer
{
    private readonly ITextureFactory _textureFactory;

    public void Render()
    {
        var texture = _textureFactory.LoadTexture("sprite.png");
        // DON'T call texture.Dispose() - factory owns it!
    }
}
```

### Caller-Owned Resources
```csharp
public class TextureLoader
{
    public static Texture Load(string path)
    {
        // Caller owns the texture
        return new Texture(path);
    }
}

// Usage - MUST dispose
public void UseTexture()
{
    using var texture = TextureLoader.Load("sprite.png");
    // texture automatically disposed at end of scope
}

// Or manual disposal
public class MyClass : IDisposable
{
    private Texture? _texture;

    public void LoadTexture(string path)
    {
        _texture = TextureLoader.Load(path);
    }

    public void Dispose()
    {
        _texture?.Dispose();
    }
}
```

## Scene Resource Management

### Entity Disposal
```csharp
// Entities contain components with resources
public class Entity
{
    public void Destroy()
    {
        // Dispose component resources
        if (HasComponent<MeshComponent>())
        {
            var mesh = GetComponent<MeshComponent>().Mesh;
            mesh?.Dispose();
        }

        // Remove from scene
        _scene.RemoveEntity(this);
    }
}
```

### Scene Disposal
```csharp
public class Scene : IDisposable
{
    private List<Entity> _entities = new();

    public void Dispose()
    {
        // Dispose all entity resources
        foreach (var entity in _entities)
        {
            entity.Destroy();
        }
        _entities.Clear();
    }
}
```

## Physics Resource Management

### Box2D Bodies
```csharp
// From PhysicsSimulationSystem - correct pattern
public class PhysicsSimulationSystem
{
    public void OnDetach(Scene scene)
    {
        // Clean up all physics bodies when scene unloads
        foreach (var entity in scene.Entities)
        {
            if (entity.HasComponent<RigidBody2DComponent>())
            {
                var rb = entity.GetComponent<RigidBody2DComponent>();
                if (rb.RuntimeBody != null)
                {
                    _world.DestroyBody(rb.RuntimeBody);
                    rb.RuntimeBody = null;
                }
            }
        }
    }
}
```

## Audio Resource Management

### OpenAL Resources
```csharp
public class AudioSource : IDisposable
{
    private uint _source;
    private uint _buffer;

    public void Dispose()
    {
        if (_source != 0)
        {
            AL.DeleteSource(_source);
            _source = 0;
        }

        if (_buffer != 0)
        {
            AL.DeleteBuffer(_buffer);
            _buffer = 0;
        }
    }
}
```

## Memory Leak Detection

### Tools
1. **dotMemory**: .NET memory profiler
2. **PerfView**: Memory allocation tracking
3. **Visual Studio Diagnostic Tools**: Memory usage graph
4. **RenderDoc**: GPU resource tracking

### Debug Helpers
```csharp
public class ResourceTracker
{
    private static int _textureCount = 0;
    private static int _bufferCount = 0;

    public static void TextureCreated() => Interlocked.Increment(ref _textureCount);
    public static void TextureDestroyed() => Interlocked.Decrement(ref _textureCount);

    public static void LogStats()
    {
        Logger.Info($"Active Textures: {_textureCount}");
        Logger.Info($"Active Buffers: {_bufferCount}");
    }
}

// In Texture constructor
public Texture()
{
    #if DEBUG
    ResourceTracker.TextureCreated();
    #endif
}

// In Dispose
public void Dispose()
{
    #if DEBUG
    ResourceTracker.TextureDestroyed();
    #endif
}
```

## Checklist for Resource Types

- [ ] Implements `IDisposable`
- [ ] Has `_disposed` guard for double-disposal
- [ ] Resets resource IDs to 0 after deletion
- [ ] Calls `GC.SuppressFinalize(this)` in Dispose
- [ ] Finalizer logs warning (doesn't call OpenGL)
- [ ] Documented who owns the resource (factory, caller, scene)
- [ ] Tested disposal in isolation
- [ ] Tested disposal in scene cleanup
- [ ] Verified no GPU memory leaks (RenderDoc)

## Output Format

**Issue**: [Resource management problem]
**Location**: [File path and line number]
**Resource Type**: [OpenGL buffer, texture, shader, etc.]
**Problem**: [Specific issue - leak, double disposal, missing cleanup]
**Recommendation**: [Fix with code example]
**Priority**: [Critical/High/Medium/Low]

### Example Output
```text
**Issue**: Mesh resources not disposed when entity is destroyed
**Location**: Engine/Scene/Entity.cs:89
**Resource Type**: OpenGL VBO, EBO, VAO
**Problem**: Entity.Destroy() doesn't dispose MeshComponent.Mesh, causing GPU memory leak
**Recommendation**:
public void Destroy()
{
    // Dispose mesh resources
    if (HasComponent<MeshComponent>())
    {
        var meshComp = GetComponent<MeshComponent>();
        meshComp.Mesh?.Dispose();
        meshComp.Mesh = null;
    }

    // Remove from scene
    _scene.RemoveEntity(this);
}

**Priority**: High (GPU memory leak)
```

## Reference Documentation
- **CLAUDE.md**: Performance and resource management guidelines
- **Disposal Patterns**: Review existing IDisposable implementations in Engine/Renderer/
- **Factory Pattern**: TextureFactory, ShaderFactory, AudioClipFactory

## Integration with Agents
This skill works with the **game-engine-expert** agent for low-level resource management and OpenGL cleanup patterns.

## Tool Restrictions
None - this skill may read code, analyze resource usage, and suggest cleanup patterns.
