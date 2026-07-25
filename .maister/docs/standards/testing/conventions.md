## Testing Conventions

### xUnit Shouldly NSubstitute Stack
Unit/integration test projects use xUnit with Microsoft.NET.Test.Sdk and xunit.runner.visualstudio; Xunit is imported globally via a project Using include.

**Sources:** code-patterns, config (confidence 91%)

```csharp
[Fact] / [Theory] tests without per-file `using Xunit;`
result.ShouldBe(expected); substitute.Received(1).Method()
```

### Test Class File Naming
Test files and classes are named `*Tests` with the class name matching the filename.

**Sources:** code-patterns (confidence 88%)

```csharp
public class TransformComponentTests { ... }
// file: TransformComponentTests.cs
```

### CI Required Test Suites
Every push/PR must restore, build (dotnet 10.0.x), and pass ECS.Tests and Engine.Tests. Engine.GraphicsTests runs when its csproj exists. Failures upload TRX artifacts.

**Sources:** ci-config (confidence 92%)

### Graphics Integration and Headless CI
CI builds with .NET 10 on ubuntu-latest, then runs ECS.Tests, Engine.Tests, and Engine.GraphicsTests. Graphics tests run under xvfb with software GL (LIBGL_ALWAYS_SOFTWARE / llvmpipe) and TRX logging; failures upload TRX artifacts.

**Sources:** ci-config, code-patterns, config (confidence 100%)

```csharp
Add new unit tests under ECS.Tests or Engine.Tests; OpenGL-dependent tests under Engine.GraphicsTests
[Trait("Category", "GraphicsIntegration")]
```
