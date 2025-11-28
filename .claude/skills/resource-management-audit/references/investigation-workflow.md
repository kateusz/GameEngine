# Resource Leak Investigation Workflow

Step-by-step process for tracking down memory leaks and GPU resource exhaustion.

## Overview

This workflow guides you through systematic investigation of resource management issues, from symptom identification to root cause fix and verification.

## Phase 1: Identify the Symptom

### 1.1 Detect the Problem

Common symptoms of resource leaks:

- **Memory growth**: Task Manager/Activity Monitor shows increasing memory usage
- **GPU memory exhaustion**: OpenGL errors like `GL_OUT_OF_MEMORY`
- **Performance degradation**: Frame rate drops over time
- **Crash on shutdown**: Disposal errors when closing application
- **Visual artifacts**: Missing textures, black screens (GPU memory full)

### 1.2 Reproduce Consistently

1. Find reliable reproduction steps
2. Measure baseline resource usage at application start
3. Perform the operation that triggers the leak
4. Measure resource usage after operation
5. Repeat 10-20 times to confirm linear growth

**Example**:
```
Start App:  GPU Memory = 200MB, CPU Memory = 150MB
Load Scene: GPU Memory = 350MB, CPU Memory = 200MB
Unload Scene: GPU Memory = 220MB (+20MB leak!), CPU Memory = 155MB
Repeat 10x: GPU Memory = 400MB (confirms 20MB leak per cycle)
```

## Phase 2: Add Resource Tracking

### 2.1 Inject ResourceTracker Calls

Add tracking to suspected resource classes (see `debug-tracking.cs`):

```csharp
#if DEBUG
ResourceTracker.TextureCreated(_rendererID);
#endif
```

Target these classes first:
1. Textures (`Texture`, `TextureFactory`)
2. Meshes (`Mesh`, `MeshRenderer`)
3. Shaders (`ShaderProgram`, `ShaderFactory`)
4. Framebuffers (`Framebuffer`, `RenderTarget`)
5. Audio (`AudioBuffer`, `AudioSource`)

### 2.2 Add Periodic Logging

In main game loop or update method:

```csharp
// Log stats every 10 seconds
private double _statsTimer = 0;

public void Update(double deltaTime)
{
    _statsTimer += deltaTime;
    if (_statsTimer >= 10.0)
    {
        #if DEBUG
        ResourceTracker.LogStats();
        #endif
        _statsTimer = 0;
    }
}
```

### 2.3 Add Shutdown Check

In application cleanup:

```csharp
public void Shutdown()
{
    #if DEBUG
    if (ResourceTracker.HasLeaks())
    {
        ResourceTracker.LogLeaks();
    }
    #endif
}
```

## Phase 3: Capture Metrics

### 3.1 Run Application with Tracking

1. Build in DEBUG mode (tracking only enabled in DEBUG)
2. Launch application
3. Perform reproduction steps
4. Watch console for ResourceTracker output every 10 seconds

**Expected Output**:
```
=== OpenGL Resource Stats ===
Textures:     15
Buffers:      8
VAOs:         4
Framebuffers: 2
Shaders:      0
Programs:     5
TOTAL:        34
```

### 3.2 Identify Growing Counters

Look for counters that increase without decreasing:

- **Normal**: Textures = 15 → 15 → 15 (stable)
- **Leak**: Buffers = 8 → 16 → 24 (growing!)

### 3.3 Correlate with Operations

Match counter changes to operations:

1. Load scene → Textures +10, Buffers +5
2. Unload scene → Textures -8 (leak of 2!), Buffers -5 (OK)
3. Conclusion: Texture cleanup is incomplete

## Phase 4: Locate the Source

### 4.1 Search for Resource Creation

Use Grep to find where the leaking resource is created:

```csharp
// If Textures are leaking:
grep "new Texture(" **/*.cs
grep "GL.GenTexture" **/*.cs
grep "TextureFactory.Load" **/*.cs
```

### 4.2 Trace Ownership Chain

For each creation site, ask:
1. Who owns this resource? (Factory, caller, scene)
2. Is there a clear disposal path?
3. Are resources cached or temporary?

**Example Trace**:
```
Scene.Load()
  → Entity.AddComponent<SpriteRenderer>()
    → TextureFactory.Load("sprite.png")
      → new Texture("sprite.png")  // Created here

Question: Who disposes this?
- TextureFactory owns it (cached) → Factory should dispose
- Or SpriteRenderer owns it? → Component should dispose
```

### 4.3 Verify Disposal Path

Check if disposal actually happens:

```csharp
// Add logging to suspected disposal method
public void Dispose()
{
    Logger.Info($"Disposing texture: {_path}");  // Add this
    if (_rendererID != 0)
    {
        GL.DeleteTexture(_rendererID);
        _rendererID = 0;
    }
}
```

Run again - if "Disposing texture" never appears, disposal isn't called!

## Phase 5: Fix the Leak

### 5.1 Identify the Pattern

Match the issue to common anti-patterns (see `anti-patterns.cs`):

1. **Missing disposal**: Resource created but never disposed
2. **Shared resource disposal**: Multiple owners, unclear who disposes
3. **Exception before disposal**: Exception thrown, Dispose() never reached
4. **Circular references**: Objects reference each other, preventing GC
5. **Event handler leaks**: Event subscriptions prevent disposal

### 5.2 Apply the Fix

**For Missing Disposal**:
```csharp
// Add Dispose() call in cleanup path
public void UnloadScene()
{
    foreach (var entity in _entities)
    {
        entity.Dispose();  // Add this
    }
}
```

**For Unclear Ownership**:
```csharp
// Document ownership explicitly
public class SpriteRenderer : IDisposable
{
    private Texture _texture;  // Owned by this component

    public void Dispose()
    {
        _texture?.Dispose();  // This component disposes it
        _texture = null;
    }
}
```

**For Exception Safety**:
```csharp
// Use try-finally or using statement
public void ProcessTexture()
{
    var texture = new Texture("temp.png");
    try
    {
        // ... operations that might throw
    }
    finally
    {
        texture.Dispose();  // Always runs
    }
}

// Or use 'using'
public void ProcessTexture()
{
    using var texture = new Texture("temp.png");
    // ... operations
} // Dispose() called automatically
```

### 5.3 Test the Fix

1. Rebuild with ResourceTracker still enabled
2. Run reproduction steps
3. Verify counter no longer grows:
   - Before fix: Textures = 15 → 17 → 19 (leak)
   - After fix: Textures = 15 → 15 → 15 (stable)

## Phase 6: Verify and Prevent

### 6.1 Comprehensive Testing

Test multiple scenarios:

- **Normal operation**: Load/unload scene 50 times
- **Rapid cycling**: Load/unload as fast as possible (stress test)
- **Editor workflow**: Create entity, add component, delete entity
- **Shutdown**: Verify `ResourceTracker.LogLeaks()` reports 0 leaks

### 6.2 Add Unit Tests

Create regression tests:

```csharp
[Test]
public void TestSceneUnloadCleansUpTextures()
{
    ResourceTracker.Reset();

    var scene = new Scene();
    scene.Load("test.json");
    int resourcesAfterLoad = ResourceTracker.GetTotalResourceCount();

    scene.Unload();
    int resourcesAfterUnload = ResourceTracker.GetTotalResourceCount();

    Assert.AreEqual(0, resourcesAfterUnload,
        "All resources should be cleaned up after scene unload");
}
```

### 6.3 Code Review Checklist

When reviewing new code that creates resources:

- [ ] Every `new Texture()` has clear disposal path
- [ ] Every `GL.GenBuffer()` has matching `GL.DeleteBuffer()`
- [ ] Factory-managed resources don't get disposed by consumers
- [ ] IDisposable implemented for all resource-owning classes
- [ ] `_disposed` flag prevents double-disposal
- [ ] `GC.SuppressFinalize(this)` called in Dispose()
- [ ] Finalizer logs warning instead of calling OpenGL

### 6.4 Document Ownership

Add XML comments documenting ownership:

```csharp
public class SpriteRenderer : IDisposable
{
    /// <summary>
    /// Texture used for rendering.
    /// OWNERSHIP: Factory-managed, do not dispose.
    /// </summary>
    private Texture _texture;

    /// <summary>
    /// Per-instance vertex buffer.
    /// OWNERSHIP: Owned by this component, disposed in Dispose().
    /// </summary>
    private uint _instanceVBO;
}
```

## Tools and Resources

### Profiling Tools

1. **dotMemory** (JetBrains): .NET memory profiler
   - Shows managed heap allocations
   - Detects leaked objects and retention paths

2. **PerfView** (Microsoft): Performance analysis tool
   - Memory allocation tracking
   - GC heap snapshots

3. **RenderDoc**: GPU debugging tool
   - Captures OpenGL state
   - Shows all active GPU resources
   - Identifies leaked textures/buffers

4. **Visual Studio Diagnostic Tools**: Built-in profiler
   - Memory usage graph
   - Snapshot comparison

### When to Use Each Tool

- **ResourceTracker**: First line of defense, quick identification
- **dotMemory**: Investigating managed object leaks (.NET objects)
- **RenderDoc**: Verifying GPU resource cleanup (OpenGL)
- **PerfView**: Analyzing large memory dumps in production

## Summary Checklist

Investigation workflow:
1. ✓ Identify symptom and reproduce consistently
2. ✓ Add ResourceTracker calls to suspected classes
3. ✓ Capture metrics during reproduction
4. ✓ Identify which resource type is leaking
5. ✓ Locate creation sites with code search
6. ✓ Trace ownership chain
7. ✓ Verify disposal path (add logging)
8. ✓ Match to anti-pattern and apply fix
9. ✓ Test fix with ResourceTracker
10. ✓ Add regression tests
11. ✓ Document ownership in code

Prevention checklist:
- [ ] Code review uses disposal checklist
- [ ] Unit tests verify cleanup in load/unload cycles
- [ ] ResourceTracker integrated in DEBUG builds
- [ ] Ownership documented in XML comments
