---
name: dependency-injection-review
description: Review code for proper dependency injection patterns using DryIoc container, ensuring no static singletons exist, constructor injection is used correctly, service lifetimes are appropriate, and all dependencies are registered in Program.cs. Use when reviewing new code, refactoring static access, or debugging DI-related issues.
---

# Dependency Injection Review

## Overview
This skill audits code for adherence to the game engine's dependency injection architecture using DryIoc. It ensures all services use constructor injection, identifies static singleton violations, and validates service registration patterns.

## When to Use
Invoke this skill when:
- Reviewing new code for DI compliance
- Refactoring static singletons to use DI
- Debugging service resolution errors
- Adding new services to the container
- Questions about service lifetime and registration
- Investigating circular dependency issues
- Validating proper disposal of services

## Core Principles

### The Golden Rule
**NEVER create static singletons!** All singleton instances must be registered in the DI container.

### Exceptions
The ONLY acceptable static classes are pure constant classes:
- `EditorUIConstants` - UI sizing and styling constants
- `RenderingConstants` - Rendering configuration constants

Everything else uses dependency injection.

## Review Checklist

### 1. Constructor Injection Pattern

**✅ CORRECT**:
```csharp
public class MyPanel : IMyPanel
{
    private readonly ISceneManager _sceneManager;
    private readonly IProjectManager _projectManager;

    public MyPanel(
        ISceneManager sceneManager,
        IProjectManager projectManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
    }

    public void DoWork()
    {
        var scene = _sceneManager.GetActiveScene();
        // Use injected dependencies
    }
}
```

**❌ WRONG**:
```csharp
// Static singleton - FORBIDDEN
public class MyPanel
{
    public static MyPanel Instance { get; } = new MyPanel();

    private MyPanel() { }

    public void DoWork()
    {
        var scene = SceneManager.Instance.GetActiveScene(); // BAD
    }
}
```

**❌ WRONG**:
```csharp
// Service locator pattern - FORBIDDEN
public class MyPanel
{
    public void DoWork()
    {
        var sceneManager = ServiceLocator.Get<ISceneManager>(); // BAD
        var scene = sceneManager.GetActiveScene();
    }
}
```

### 2. Service Registration

**Location**: `Editor/Program.cs` or `Engine/Program.cs`

**✅ CORRECT**:
```csharp
private static void ConfigureServices(Container container)
{
    // Managers (Singleton - one instance per application)
    container.Register<ISceneManager, SceneManager>(Reuse.Singleton);
    container.Register<IProjectManager, ProjectManager>(Reuse.Singleton);

    // Factories (Singleton - stateless factories can be shared)
    container.Register<ITextureFactory, TextureFactory>(Reuse.Singleton);
    container.Register<IShaderFactory, ShaderFactory>(Reuse.Singleton);

    // Panels (Singleton - one instance per editor session)
    container.Register<ISceneHierarchyPanel, SceneHierarchyPanel>(Reuse.Singleton);
    container.Register<IPropertiesPanel, PropertiesPanel>(Reuse.Singleton);

    // Systems (Singleton - registered once per scene)
    container.Register<AnimationSystem>(Reuse.Singleton);
    container.Register<PhysicsSimulationSystem>(Reuse.Singleton);

    // Transient services (new instance per request - rare)
    container.Register<ISceneSerializer, SceneSerializer>(Reuse.Transient);
}
```

**Service Lifetime Guidelines**:
- **Singleton** (default): Managers, factories, panels, systems, stateful services
- **Transient**: Services that maintain per-operation state or are lightweight
- **Scoped**: Not typically used in this engine (no request scope)

### 3. Interface-Based Design

**✅ CORRECT**:
```csharp
// Define interface
public interface ISceneManager
{
    Scene? GetActiveScene();
    void SetActiveScene(Scene scene);
}

// Implement interface
public class SceneManager : ISceneManager
{
    private Scene? _activeScene;

    public Scene? GetActiveScene() => _activeScene;

    public void SetActiveScene(Scene scene)
    {
        _activeScene = scene;
    }
}

// Register mapping
container.Register<ISceneManager, SceneManager>(Reuse.Singleton);

// Inject interface
public class MyPanel
{
    public MyPanel(ISceneManager sceneManager) { }
}
```

**When to skip interfaces**:
- Concrete classes with no abstraction needs (some systems, POCOs)
- Internal implementation details not exposed to other modules
- Pure data classes or components

### 4. Circular Dependency Detection

**❌ FORBIDDEN**:
```csharp
// Service A depends on Service B
public class ServiceA
{
    public ServiceA(IServiceB serviceB) { }
}

// Service B depends on Service A - CIRCULAR!
public class ServiceB
{
    public ServiceB(IServiceA serviceA) { }
}
```

**✅ SOLUTIONS**:

**Option 1: Extract shared dependency**
```csharp
public class ServiceA
{
    public ServiceA(ISharedService shared) { }
}

public class ServiceB
{
    public ServiceB(ISharedService shared) { }
}
```

**Option 2: Use events for decoupling**
```csharp
public class ServiceA
{
    public event Action<Data>? OnDataChanged;
}

public class ServiceB
{
    public ServiceB(IServiceA serviceA)
    {
        serviceA.OnDataChanged += HandleDataChanged;
    }
}
```

**Option 3: Lazy resolution (last resort)**
```csharp
public class ServiceA
{
    private readonly Lazy<IServiceB> _serviceB;

    public ServiceA(Lazy<IServiceB> serviceB)
    {
        _serviceB = serviceB;
    }

    public void DoWork()
    {
        _serviceB.Value.DoSomething();
    }
}
```

### 5. Null Reference Validation

**✅ ALWAYS validate injected dependencies**:
```csharp
public MyClass(
    ISceneManager sceneManager,
    IProjectManager projectManager)
{
    _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
}
```

**Alternative (C# 11+)**:
```csharp
public MyClass(
    ISceneManager sceneManager,
    IProjectManager projectManager)
{
    ArgumentNullException.ThrowIfNull(sceneManager);
    ArgumentNullException.ThrowIfNull(projectManager);

    _sceneManager = sceneManager;
    _projectManager = projectManager;
}
```

### 6. Disposal and Resource Management

**IDisposable services**:
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
        // ... implementation
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

// Container automatically disposes singletons implementing IDisposable
```

**Injecting IDisposable**:
```csharp
// ✅ CORRECT - Injected disposables managed by container
public class MyPanel
{
    private readonly ITextureFactory _textureFactory;

    public MyPanel(ITextureFactory textureFactory)
    {
        _textureFactory = textureFactory;
    }

    // DO NOT dispose injected dependencies
    // Container manages their lifetime
}

// ❌ WRONG - Don't dispose injected dependencies
public class MyPanel : IDisposable
{
    private readonly ITextureFactory _textureFactory;

    public void Dispose()
    {
        _textureFactory.Dispose(); // WRONG! Container manages this
    }
}
```

## Common DI Anti-Patterns

### 1. Service Locator
```csharp
// ❌ WRONG
public class MyClass
{
    public void DoWork()
    {
        var manager = Container.Resolve<ISceneManager>(); // BAD
    }
}

// ✅ CORRECT
public class MyClass
{
    private readonly ISceneManager _manager;

    public MyClass(ISceneManager manager)
    {
        _manager = manager;
    }

    public void DoWork()
    {
        _manager.DoSomething();
    }
}
```

### 2. Static State
```csharp
// ❌ WRONG
public static class TextureCache
{
    private static Dictionary<string, Texture> _cache = new();

    public static Texture Get(string path)
    {
        // ... WRONG - use DI factory instead
    }
}

// ✅ CORRECT
public interface ITextureFactory
{
    Texture LoadTexture(string path);
}

public class TextureFactory : ITextureFactory
{
    private readonly Dictionary<string, Texture> _cache = new();

    public Texture LoadTexture(string path)
    {
        // ... implementation with caching
    }
}
```

### 3. New Keyword for Services
```csharp
// ❌ WRONG
public class MyPanel
{
    public void DoWork()
    {
        var serializer = new SceneSerializer(); // WRONG
        serializer.Save(scene);
    }
}

// ✅ CORRECT
public class MyPanel
{
    private readonly ISceneSerializer _serializer;

    public MyPanel(ISceneSerializer serializer)
    {
        _serializer = serializer;
    }

    public void DoWork()
    {
        _serializer.Save(scene);
    }
}
```

### 4. Property Injection
```csharp
// ❌ WRONG - Property injection (avoid in this engine)
public class MyPanel
{
    public ISceneManager SceneManager { get; set; } // BAD
}

// ✅ CORRECT - Constructor injection
public class MyPanel
{
    private readonly ISceneManager _sceneManager;

    public MyPanel(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }
}
```

## Registration Patterns

### Registering with Dependencies
```csharp
// Service with dependencies
public class AnimationSystem
{
    public AnimationSystem(ITextureFactory textureFactory) { }
}

// Simple registration - DryIoc auto-resolves dependencies
container.Register<AnimationSystem>(Reuse.Singleton);
```

### Registering with Setup
```csharp
// Service needing initialization
container.Register<ISceneManager, SceneManager>(
    Reuse.Singleton,
    setup: Setup.With(allowDisposableTransient: true));
```

### Registering Multiple Implementations
```csharp
// Register named implementations
container.Register<IRenderer, Renderer2D>(Reuse.Singleton, serviceKey: "2D");
container.Register<IRenderer, Renderer3D>(Reuse.Singleton, serviceKey: "3D");

// Resolve specific implementation
public MyClass(
    [Named("2D")] IRenderer renderer2D,
    [Named("3D")] IRenderer renderer3D)
{
}
```

## Debugging DI Issues

### Common Errors and Solutions

**Error**: "No service registered for type X"
- **Solution**: Add registration to `Program.cs` ConfigureServices

**Error**: "Circular dependency detected"
- **Solution**: Refactor to extract shared dependency or use events

**Error**: "Service is null after construction"
- **Solution**: Ensure service is registered before dependent services

**Error**: "Multiple constructors found"
- **Solution**: Use `[DryIoc.Attributes.Constructor]` attribute on preferred constructor

**Error**: "Disposed object accessed"
- **Solution**: Check service lifetimes - don't inject transient into singleton

## Testing with DI

```csharp
public class MyPanelTests
{
    [Fact]
    public void TestPanelLogic()
    {
        // Arrange - Mock dependencies
        var mockSceneManager = new Mock<ISceneManager>();
        mockSceneManager.Setup(m => m.GetActiveScene())
            .Returns(new Scene("Test"));

        // Act - Inject mocks
        var panel = new MyPanel(mockSceneManager.Object);
        panel.DoWork();

        // Assert
        mockSceneManager.Verify(m => m.GetActiveScene(), Times.Once);
    }
}
```

## Output Format

**Issue**: [DI violation description]
**Location**: [File path and line number]
**Problem**: [Specific anti-pattern or violation]
**Recommendation**: [How to fix with code example]
**Priority**: [Critical/High/Medium/Low]

### Example Output
```text
**Issue**: Static singleton pattern detected
**Location**: Editor/Managers/AssetManager.cs:15
**Problem**: Class uses static Instance property instead of DI
**Recommendation**:
// Remove static singleton
public class AssetManager : IAssetManager
{
    // Remove: public static AssetManager Instance { get; } = new();

    // Add constructor for DI
    public AssetManager(IProjectManager projectManager)
    {
        // ... initialization
    }
}

// Register in Program.cs
container.Register<IAssetManager, AssetManager>(Reuse.Singleton);

// Inject where needed
public class MyPanel
{
    public MyPanel(IAssetManager assetManager) { }
}

**Priority**: High
```

## Reference Documentation
- **Architecture**: `CLAUDE.md` - Dependency Injection patterns
- **DryIoc Docs**: DryIoc container documentation
- **Program.cs**: `Editor/Program.cs` - All service registrations (50+ services)
- **Existing Services**: Review existing managers, factories, panels for patterns

## Integration with Agents
This skill works across all agents. Use for architectural compliance review regardless of domain (engine, editor, or game development).

## Tool Restrictions
None - this skill may read code, analyze architecture, and suggest refactorings.
