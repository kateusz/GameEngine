# Common Mistakes

### Mismatched test namespace

**Problem**: Test file in the wrong namespace causes confusion and inconsistent convention.

**Fix**: Mirror source namespace replacing `Engine` with `Engine.Tests` (e.g., `Engine.Scene.Systems` → `Engine.Tests.Systems`).

### Not mirroring subdirectory structure

**Problem**: `Engine/Renderer/Shader.cs` tested by a file in `tests/Engine.Tests/` root instead of `tests/Engine.Tests/Renderer/`.

**Fix**: Match the relative path under the source project root.

### Writing tests for data-only components

**Problem**: Components like `VelocityComponent(Vector2 Velocity)` have no logic to test.

**Fix**: Skip record structs that are purely data. Only test components with behavior (computed properties, validation, serialization logic).

### Using xUnit Assert instead of Shouldly

**Problem**: Using xUnit `Assert.Equal`, `Assert.True`, etc. instead of the project-wide Shouldly convention.

**Fix**: Use Shouldly in all test projects. See [conventions.md](conventions.md#assertions).

### Forgetting to add test file to .csproj

**Problem**: Test file not compiled because it's not included in the project.

**Fix**: Ensure `.csproj` uses SDK-style globbing (default in .NET 10+) — no manual file listing needed.

### Treating grouped tests as missing

**Problem**: Flagging a type as uncovered when it lives in a feature-area file (e.g., `SerializationTests.cs`).

**Fix**: Run grouped-test detection from Step 4 before reporting missing.
