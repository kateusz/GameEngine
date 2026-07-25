## Resources and Logging

### Never Call OpenGL in Finalizers
Do not call GL delete/cleanup in finalizers (no GL context on finalizer thread). Log GPU leaks in finalizers; perform GL cleanup only in Dispose on the owning thread.

**Sources:** documentation (confidence 92%)

```csharp
Finalizer: log error if _rendererID != 0; do NOT call GL.DeleteTexture
Dispose: GL.DeleteTexture(_rendererID); _rendererID = 0; GC.SuppressFinalize(this)
```

### Disposal Guards Required
Always guard double-disposal with a _disposed flag, check resource handle != 0, reset handle after delete, and call GC.SuppressFinalize(this) in Dispose.

**Sources:** documentation (confidence 90%)

```csharp
if (_disposed) return; if (_rendererID != 0) { GL.Delete...; _rendererID = 0; } _disposed = true; GC.SuppressFinalize(this);
```

### Factory Owns Cached Resources
Factory-managed/cached resources must not be disposed by consumers. Owned resources created exclusively by a component must be disposed by that owner.

**Sources:** documentation (confidence 90%)

```csharp
Wrong: texture from TextureFactory then texture.Dispose()
Correct: use texture; factory disposes on shutdown
```

### Serialize Paths Not Loaded Resources
Components store asset path strings, not GPU/audio/model instances. Mark runtime objects with [JsonIgnore]; systems resolve resources via factories at runtime.

**Sources:** documentation (confidence 92%)

```csharp
public string TexturePath { get; set; } = string.Empty;
[JsonIgnore] public Texture? LoadedTexture { get; set; }
```

### Serilog Logging Stack
Engine standardizes on Serilog with console, file, async, and thread enricher sinks for application logging.

**Sources:** config (confidence 80%)

```csharp
Use Serilog ILogger/Log rather than introducing a second logging framework
```

### Unsafe Code for Interop Projects
Engine, runtime/player, ImGui UI, and graphics-related executables/tests allow unsafe blocks for native/GPU interop. Pure libraries (ECS, Math, Input, Audio, Scripting, games) do not enable unsafe.

**Sources:** config (confidence 80%)

```csharp
Enable AllowUnsafeBlocks only in projects that touch OpenGL/native interop
```
