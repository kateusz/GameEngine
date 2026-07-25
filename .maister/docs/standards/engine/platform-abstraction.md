## Platform Abstraction

### Platform Abstraction Boundary
All platform-specific code must live under Engine/Platform/{PlatformName}/. Engine core must use IRendererAPI/IShader/ITexture2D interfaces only — never import Silk.NET.* or instantiate SilkNet* types outside Platform.

**Sources:** code-patterns, documentation (confidence 94%)

```csharp
internal sealed class Graphics2D(IRendererAPI rendererApi, ...) : IGraphics2D
rendererApi.DrawIndexed(...); // not GL.DrawElements
```

### GL Error Checks After Platform GL Calls
Platform SilkNet code should use SilkNetContext.GL and call GLDebug.CheckError() after GL calls.

**Sources:** documentation (confidence 88%)

### Automated Engine Review Criteria
Claude Code Review on every PR opened/synchronized must evaluate ECS separation, frame budgets (16ms/60fps or 8ms/120fps), OpenGL via Silk.NET abstractions, fixed-timestep physics, resource lifetime/IDisposable, and avoid magic numbers — using CLAUDE.md conventions.

**Sources:** ci-config (confidence 90%)
