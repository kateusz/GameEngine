// Resource Management Anti-Patterns
// Common mistakes and their corrections for OpenGL resource cleanup

using System;
using System.Collections.Generic;
using OpenGL; // Placeholder - actual project uses Silk.NET.OpenGL

// ============================================================================
// ANTI-PATTERN 1: Missing Disposal
// ============================================================================

// ❌ WRONG - Texture never disposed, GPU memory leak
public class BadTextureLoader
{
    public void LoadTexture(string path)
    {
        var texture = new Texture(path);
        // texture goes out of scope without being disposed
        // GPU memory leak every time this is called!
    }
}

// ✅ CORRECT - Return texture for caller to manage
public class GoodTextureLoader_Option1
{
    public Texture LoadTexture(string path)
    {
        // Caller is responsible for disposal
        return new Texture(path);
    }
}

// ✅ CORRECT - Use factory with caching and disposal
public class GoodTextureLoader_Option2
{
    private TextureFactory _factory;

    public Texture LoadTexture(string path)
    {
        // Factory manages lifetime and disposal
        return _factory.Load(path);
    }
}

// ============================================================================
// ANTI-PATTERN 2: Double Disposal
// ============================================================================

// ❌ WRONG - Can crash if Dispose() called twice
public class UnsafeTexture : IDisposable
{
    private uint _rendererID;

    public void Dispose()
    {
        GL.DeleteTexture(_rendererID); // Crashes if called twice!
        // _rendererID still contains old value
    }
}

// ✅ CORRECT - Guard against double disposal
public class SafeTexture : IDisposable
{
    private uint _rendererID;
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;  // Early exit if already disposed

        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;  // Reset to prevent double-delete
        }

        _disposed = true;
    }
}

// ============================================================================
// ANTI-PATTERN 3: Disposing Shared Resources
// ============================================================================

// ❌ WRONG - Multiple objects share same GPU buffer
public class BadModelInstance : IDisposable
{
    private Mesh _sharedMesh; // Reference to shared mesh!

    public BadModelInstance(Mesh mesh)
    {
        _sharedMesh = mesh; // This is shared with other instances
    }

    public void Dispose()
    {
        _sharedMesh.Dispose(); // WRONG - other instances still using it!
    }
}

// ✅ CORRECT - Don't dispose shared resources
public class GoodModelInstance : IDisposable
{
    private Mesh _sharedMesh; // Reference, not owned

    public GoodModelInstance(Mesh mesh)
    {
        _sharedMesh = mesh;
    }

    public void Dispose()
    {
        // Don't dispose _sharedMesh - it's managed by factory/pool
        // Only dispose resources THIS instance owns
        // (e.g., per-instance transform buffer, materials, etc.)
    }
}

// ============================================================================
// ANTI-PATTERN 4: OpenGL Calls in Finalizers
// ============================================================================

// ❌ WRONG - OpenGL not safe in finalizer
public class BadTexture : IDisposable
{
    private uint _rendererID;

    ~BadTexture()
    {
        GL.DeleteTexture(_rendererID); // CRASH! No GL context on finalizer thread
    }
}

// ✅ CORRECT - Log warning instead of calling OpenGL
public class GoodTexture : IDisposable
{
    private uint _rendererID;
    private string _path; // For debugging

    ~GoodTexture()
    {
        if (_rendererID != 0)
        {
            Logger.Error($"Texture {_path} was not properly disposed! GPU memory leak.");
            // Don't call GL functions in finalizer!
            // This warning helps developers find the leak source
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

// ============================================================================
// ANTI-PATTERN 5: Forgetting GC.SuppressFinalize
// ============================================================================

// ❌ WRONG - Finalizer still runs even after proper disposal
public class IneffcientDisposal : IDisposable
{
    private uint _rendererID;

    public void Dispose()
    {
        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;
        }
        // Missing GC.SuppressFinalize(this)!
        // Finalizer will still be queued unnecessarily
    }

    ~IneffcientDisposal()
    {
        // This runs even after Dispose() was called properly
        // Wastes GC resources
    }
}

// ✅ CORRECT - Always call GC.SuppressFinalize
public class EfficientDisposal : IDisposable
{
    private uint _rendererID;

    public void Dispose()
    {
        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;
        }
        GC.SuppressFinalize(this); // Tell GC to skip finalizer
    }

    ~EfficientDisposal()
    {
        // This only runs if Dispose() was NOT called
        Logger.Warning("Resource not disposed properly!");
    }
}

// ============================================================================
// ANTI-PATTERN 6: Disposing in Wrong Order
// ============================================================================

// ❌ WRONG - Disposing framebuffer before its attachments
public class BadFramebuffer : IDisposable
{
    private uint _fboID;
    private uint _colorTextureID;
    private uint _depthRboID;

    public void Dispose()
    {
        // Wrong order! Should delete attachments first
        GL.DeleteFramebuffer(_fboID);
        GL.DeleteTexture(_colorTextureID);
        GL.DeleteRenderbuffer(_depthRboID);
    }
}

// ✅ CORRECT - Delete in reverse order of creation
public class GoodFramebuffer : IDisposable
{
    private uint _fboID;
    private uint _colorTextureID;
    private uint _depthRboID;
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        // Delete attachments first (reverse order of creation)
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

        // Then delete the framebuffer itself
        if (_fboID != 0)
        {
            GL.DeleteFramebuffer(_fboID);
            _fboID = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// ============================================================================
// ANTI-PATTERN 7: Not Checking for Null Before Disposing
// ============================================================================

// ❌ WRONG - NullReferenceException if mesh is null
public class BadEntityCleanup
{
    private Mesh _mesh;

    public void Cleanup()
    {
        _mesh.Dispose(); // Crash if _mesh is null!
    }
}

// ✅ CORRECT - Null-conditional operator
public class GoodEntityCleanup
{
    private Mesh _mesh;

    public void Cleanup()
    {
        _mesh?.Dispose(); // Safe - no-op if null
        _mesh = null;      // Clear reference
    }
}

// ============================================================================
// ANTI-PATTERN 8: Creating Resources Without Disposal Path
// ============================================================================

// ❌ WRONG - No clear disposal path
public class BadRenderer
{
    public void RenderFrame()
    {
        // Creating temporary framebuffer every frame!
        var tempFBO = new Framebuffer(1920, 1080);
        // ... render to FBO
        // tempFBO never disposed - massive leak!
    }
}

// ✅ CORRECT - Cache or explicitly dispose
public class GoodRenderer
{
    private Framebuffer _cachedFBO; // Reuse across frames

    public void Initialize()
    {
        _cachedFBO = new Framebuffer(1920, 1080);
    }

    public void RenderFrame()
    {
        // Reuse cached framebuffer
        _cachedFBO.Bind();
        // ... render
    }

    public void Dispose()
    {
        _cachedFBO?.Dispose();
    }
}
