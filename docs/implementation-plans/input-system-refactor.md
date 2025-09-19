# Input System Refactoring Plan - Final Design

## Current Architecture Problems

### SilkNetGameWindow Analysis
```csharp
public class SilkNetGameWindow : IGameWindow
{
    // ❌ Static dependencies - global state
    public static IKeyboard Keyboard { get; private set; } = null!;
    public static IMouse Mouse { get; private set; } = null!;

    // ❌ Mixed responsibilities - window + input + OpenGL
    private void WindowOnLoad()
    {
        SilkNetContext.GL = _window.CreateOpenGL();        // OpenGL concern
        SilkNetContext.InputContext = _window.CreateInput(); // Input concern
        Mouse = SilkNetContext.InputContext.Mice[0];       // Static assignment
    }

    // ❌ Single event pipeline - mixes window and input events
    public event Action<Event> OnEvent = null!;

    private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        OnEvent(new KeyPressedEvent((int)key, true)); // Input event
    }

    private void OnFrameBufferResize(Vector2D<int> newSize)
    {
        OnEvent(new WindowResizeEvent(newSize.X, newSize.Y)); // Window event
    }
}
```

### Core Issues Identified
1. **Violation of Single Responsibility**: Window class handles windowing, input, and OpenGL
2. **Static Dependencies**: Global state prevents testing and creates tight coupling
3. **Mixed Event Types**: Single event pipeline reduces performance and clarity
4. **Resource Management**: Manual disposal scattered across different concerns
5. **Testing Challenges**: Static access makes unit testing impossible

---

## Final Design - Clean Architecture

### Core Interfaces

```csharp
// Engine/Core/Input/IInputSystem.cs
public interface IInputSystem : IDisposable
{
    IKeyboard Keyboard { get; }
    IMouse Mouse { get; }
    void Update(TimeSpan deltaTime);
    event Action<InputEvent> InputReceived;
}

// Engine/Core/Input/IKeyboard.cs
public interface IKeyboard
{
    bool IsKeyPressed(KeyCode key);
    bool IsKeyDown(KeyCode key);
    bool IsKeyUp(KeyCode key);
}

// Engine/Core/Input/IMouse.cs
public interface IMouse
{
    Vector2 Position { get; }
    bool IsButtonPressed(MouseButton button);
    bool IsButtonDown(MouseButton button);
    bool IsButtonUp(MouseButton button);
    float ScrollWheel { get; }
}
```

### Window Interface

```csharp
// Engine/Core/Window/IGameWindow.cs
public interface IGameWindow : IDisposable
{
    event Action<WindowEvent> OnWindowEvent;  // Resize, close, focus, etc.
    event Action<InputEvent> OnInputEvent;    // Keys, mouse, gamepad, etc.
    event Action OnUpdate;
    event Action OnWindowLoad;

    void Run();
}
```

### Layer Interface

```csharp
// Engine/Core/ILayer.cs
public interface ILayer : IDisposable
{
    void OnAttach();
    void OnDetach();
    void OnUpdate(TimeSpan deltaTime);
    void OnImGuiRender();

    void HandleInputEvent(InputEvent inputEvent);
    void HandleWindowEvent(WindowEvent windowEvent);

    bool WantsInputEvents { get; }
    bool WantsWindowEvents { get; }
}
```

---

## Phase 1: Platform Implementation

### SilkNet Input System with Concurrent Queue
```csharp
// Engine/Platform/SilkNet/Input/SilkNetInputSystem.cs
public class SilkNetInputSystem : IInputSystem
{
    private readonly Silk.NET.Input.IInputContext _inputContext;
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    private volatile bool _disposed;

    public SilkNetInputSystem(Silk.NET.Input.IInputContext inputContext)
    {
        _inputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));

        // Initialize devices immediately
        var silkKeyboard = _inputContext.Keyboards.FirstOrDefault()
            ?? throw new InvalidOperationException("No keyboard found");
        var silkMouse = _inputContext.Mice.FirstOrDefault()
            ?? throw new InvalidOperationException("No mouse found");

        _keyboard = new SilkNetKeyboard(silkKeyboard);
        _mouse = new SilkNetMouse(silkMouse);

        // Subscribe to SilkNet input events
        silkKeyboard.KeyDown += OnSilkKeyDown;
        silkKeyboard.KeyUp += OnSilkKeyUp;
        silkMouse.MouseDown += OnSilkMouseDown;
        silkMouse.MouseUp += OnSilkMouseUp;
        silkMouse.Scroll += OnSilkMouseScroll;
    }

    public IKeyboard Keyboard => _keyboard;
    public IMouse Mouse => _mouse;

    public void Update(TimeSpan deltaTime)
    {
        // Process all queued input events
        while (_inputQueue.TryDequeue(out var inputEvent))
        {
            InputReceived?.Invoke(inputEvent);
        }
    }

    public event Action<InputEvent>? InputReceived;

    private void OnSilkKeyDown(Silk.NET.Input.IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyPressedEvent((int)key, false);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkKeyUp(Silk.NET.Input.IKeyboard keyboard, Key key, int keyCode)
    {
        if (_disposed) return;

        var inputEvent = new KeyReleasedEvent((int)key);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseDown(Silk.NET.Input.IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonPressedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseUp(Silk.NET.Input.IMouse mouse, MouseButton button)
    {
        if (_disposed) return;

        var inputEvent = new MouseButtonReleasedEvent((int)button);
        _inputQueue.Enqueue(inputEvent);
    }

    private void OnSilkMouseScroll(Silk.NET.Input.IMouse mouse, ScrollWheel scrollWheel)
    {
        if (_disposed) return;

        var inputEvent = new MouseScrolledEvent(scrollWheel.X, scrollWheel.Y);
        _inputQueue.Enqueue(inputEvent);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _inputContext?.Dispose();
        }
    }
}
```

### Device Implementations
```csharp
// Engine/Platform/SilkNet/Input/SilkNetKeyboard.cs
public class SilkNetKeyboard : IKeyboard
{
    private readonly Silk.NET.Input.IKeyboard _silkKeyboard;

    public SilkNetKeyboard(Silk.NET.Input.IKeyboard silkKeyboard)
    {
        _silkKeyboard = silkKeyboard ?? throw new ArgumentNullException(nameof(silkKeyboard));
    }

    public bool IsKeyPressed(KeyCode keyCode)
    {
        return _silkKeyboard.IsKeyPressed((Key)keyCode);
    }

    public bool IsKeyDown(KeyCode keyCode)
    {
        return _silkKeyboard.IsKeyPressed((Key)keyCode);
    }

    public bool IsKeyUp(KeyCode keyCode)
    {
        return !_silkKeyboard.IsKeyPressed((Key)keyCode);
    }
}

// Engine/Platform/SilkNet/Input/SilkNetMouse.cs
public class SilkNetMouse : IMouse
{
    private readonly Silk.NET.Input.IMouse _silkMouse;

    public SilkNetMouse(Silk.NET.Input.IMouse silkMouse)
    {
        _silkMouse = silkMouse ?? throw new ArgumentNullException(nameof(silkMouse));
    }

    public Vector2 Position => new(_silkMouse.Position.X, _silkMouse.Position.Y);

    public bool IsButtonPressed(MouseButton button)
    {
        return _silkMouse.IsButtonPressed((Silk.NET.Input.MouseButton)button);
    }

    public bool IsButtonDown(MouseButton button)
    {
        return _silkMouse.IsButtonPressed((Silk.NET.Input.MouseButton)button);
    }

    public bool IsButtonUp(MouseButton button)
    {
        return !_silkMouse.IsButtonPressed((Silk.NET.Input.MouseButton)button);
    }

    public float ScrollWheel { get; private set; }
}
```

---

## Phase 2: Refactor SilkNetGameWindow

### Clean Separation of Concerns
```csharp
// Engine/Platform/SilkNet/SilkNetGameWindow.cs
public class SilkNetGameWindow : IGameWindow
{
    private readonly IWindow _window;
    private readonly IInputSystem _inputSystem;
    private bool _disposed;

    public SilkNetGameWindow(IWindow window, IInputSystem inputSystem)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _inputSystem = inputSystem ?? throw new ArgumentNullException(nameof(inputSystem));

        _window.WindowState = WindowState.Maximized;

        // Only window-related events
        _window.Load += WindowOnLoad;
        _window.Update += WindowOnUpdate;
        _window.Closing += OnWindowClosing;
        _window.FramebufferResize += OnFrameBufferResize;

        // Input events handled by input system
        _inputSystem.InputReceived += OnInputReceived;
    }

    public event Action<WindowEvent>? OnWindowEvent;
    public event Action<InputEvent>? OnInputEvent;
    public event Action? OnUpdate;
    public event Action? OnWindowLoad;

    public void Run()
    {
        _window.Run();
    }

    private void WindowOnLoad()
    {
        // Only window and OpenGL concerns
        SilkNetContext.GL = _window.CreateOpenGL();
        SilkNetContext.Window = _window;

        Console.WriteLine("Window loaded!");
        OnWindowLoad?.Invoke();
    }

    private void WindowOnUpdate(double deltaTime)
    {
        OnUpdate?.Invoke();
    }

    private void OnWindowClosing()
    {
        var closeEvent = new WindowCloseEvent();
        OnWindowEvent?.Invoke(closeEvent);
    }

    private void OnFrameBufferResize(Vector2D<int> newSize)
    {
        SilkNetContext.GL.Viewport(newSize);

        var resizeEvent = new WindowResizeEvent(newSize.X, newSize.Y);
        OnWindowEvent?.Invoke(resizeEvent);
    }

    private void OnInputReceived(InputEvent inputEvent)
    {
        OnInputEvent?.Invoke(inputEvent);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _inputSystem?.Dispose();
            SilkNetContext.GL?.Dispose();
            _window?.Dispose();
            _disposed = true;
        }
    }
}
```

---

## Phase 3: Update Application Architecture

### Application Class
```csharp
// Engine/Core/Application.cs
public abstract class Application : IDisposable
{
    private readonly IGameWindow _gameWindow;
    private readonly IInputSystem _inputSystem;
    private readonly IImGuiLayer _imGuiLayer;
    private readonly LayerStack _layerStack = new();

    protected Application(IGameWindow gameWindow, IInputSystem inputSystem, IImGuiLayer imGuiLayer)
    {
        _gameWindow = gameWindow ?? throw new ArgumentNullException(nameof(gameWindow));
        _inputSystem = inputSystem ?? throw new ArgumentNullException(nameof(inputSystem));
        _imGuiLayer = imGuiLayer ?? throw new ArgumentNullException(nameof(imGuiLayer));

        // Subscribe to separated event types
        _gameWindow.OnWindowEvent += HandleWindowEvent;
        _gameWindow.OnInputEvent += HandleInputEvent;
        _gameWindow.OnUpdate += Update;
    }

    protected void Update()
    {
        // Update input system - processes queued events
        _inputSystem.Update(TimeSpan.FromMilliseconds(16)); // TODO: Real delta time

        // Update layers
        foreach (var layer in _layerStack)
        {
            layer.OnUpdate(TimeSpan.FromMilliseconds(16));
        }
    }

    private void HandleInputEvent(InputEvent inputEvent)
    {
        // Input events handled in reverse order (overlay layers first)
        foreach (var layer in _layerStack.GetOverlayLayers().Reverse())
        {
            if (!layer.WantsInputEvents) continue;

            layer.HandleInputEvent(inputEvent);
            if (inputEvent.IsHandled) return;
        }

        // Then regular layers
        foreach (var layer in _layerStack.GetLayers().Reverse())
        {
            if (!layer.WantsInputEvents) continue;

            layer.HandleInputEvent(inputEvent);
            if (inputEvent.IsHandled) return;
        }
    }

    private void HandleWindowEvent(WindowEvent windowEvent)
    {
        // Window events affect all layers that want them
        foreach (var layer in _layerStack)
        {
            if (layer.WantsWindowEvents)
            {
                layer.HandleWindowEvent(windowEvent);
            }
        }
    }

    public void Dispose()
    {
        _layerStack?.Dispose();
        _inputSystem?.Dispose();
        _gameWindow?.Dispose();
    }
}
```

---

## Phase 4: DI Registration

### Container Setup
```csharp
// Editor/Program.cs, Sandbox/Program.cs, Benchmark/Program.cs
public static void ConfigureContainer(Container container)
{
    // Register input system with proper lifecycle
    container.Register<IInputSystem>(
        made: Made.Of(() => CreateInputSystem(Arg.Of<IWindow>())),
        Reuse.Singleton);

    // Register window with input system dependency
    container.Register<IGameWindow>(
        made: Made.Of(() => new SilkNetGameWindow(
            Arg.Of<IWindow>(),
            Arg.Of<IInputSystem>())),
        Reuse.Singleton);

    // Register application with all dependencies
    container.Register<Application>(
        made: Made.Of(() => new SandboxApplication(
            Arg.Of<IGameWindow>(),
            Arg.Of<IInputSystem>(),
            Arg.Of<IImGuiLayer>())),
        Reuse.Singleton);
}

private static IInputSystem CreateInputSystem(IWindow window)
{
    var inputContext = window.CreateInput();
    return new SilkNetInputSystem(inputContext);
}
```

---

## Phase 5: Migration Strategy

### Step 1: Create Core Interfaces
```csharp
// Engine/Core/Input/IInputSystem.cs
// Engine/Core/Input/IKeyboard.cs
// Engine/Core/Input/IMouse.cs
```

### Step 2: Implement Platform Code
```csharp
// Engine/Platform/SilkNet/Input/SilkNetInputSystem.cs
// Engine/Platform/SilkNet/Input/SilkNetKeyboard.cs
// Engine/Platform/SilkNet/Input/SilkNetMouse.cs
```

### Step 3: Update Window Interface and Implementation
```csharp
// Update IGameWindow interface - add event separation
// Update SilkNetGameWindow implementation - remove static input
```

### Step 4: Update Application Layer
```csharp
// Update Application constructor - inject IInputSystem
// Update ILayer interface - separate event methods
// Update all layer implementations
```

### Step 5: Update DI Registration
```csharp
// Update Program.cs files
// Register IInputSystem
// Update constructors
```

### Step 6: Replace Static Usage
```csharp
// Replace all InputState.Instance usage
// Replace all SilkNetGameWindow.Keyboard usage
// Update game scripts and layers
```

### Step 7: Cleanup
```csharp
// Remove static properties from SilkNetGameWindow
// Remove InputState.Instance
```

---

## SOLID Principles Achieved

✅ **Single Responsibility**:
- `SilkNetGameWindow`: Only handles window concerns
- `SilkNetInputSystem`: Only handles input concerns
- `Application`: Only handles application lifecycle and event coordination

✅ **Open/Closed**:
- Easy to add `XInputInputSystem`, `DirectInputSystem`
- New input devices supported without changing existing code
- Event types can be extended without breaking layers

✅ **Liskov Substitution**:
- Any `IInputSystem` implementation works identically
- Platform switching is completely transparent
- Layers work with any input system implementation

✅ **Interface Segregation**:
- Layers can depend on just `IKeyboard` if needed
- Event separation allows performance optimizations
- No forced dependencies on unused functionality

✅ **Dependency Inversion**:
- Game code depends on `IInputSystem` abstraction
- Platform-specific code isolated in Platform folder
- Easy to mock for unit testing

---

## Performance Benefits

### Event Processing
- **Thread-Safe**: `ConcurrentQueue` handles multi-threaded input callbacks
- **Batched Processing**: All input events processed during `Update()` call
- **No Blocking**: Input callbacks never block, just enqueue events

### Event Filtering
```csharp
public class GameplayLayer : ILayer
{
    public bool WantsInputEvents => true;   // Needs input
    public bool WantsWindowEvents => false; // Doesn't care about window resize
}

public class EditorViewportLayer : ILayer
{
    public bool WantsInputEvents => true;  // Mouse interaction
    public bool WantsWindowEvents => true; // Viewport resizing
}
```

### Input Prioritization
```csharp
// UI layers handle input first (ImGui, overlays)
foreach (var layer in overlayLayers.Reverse())
{
    if (!layer.WantsInputEvents) continue;
    layer.HandleInputEvent(inputEvent);
    if (inputEvent.IsHandled) return; // Stop propagation
}

// Then game layers
foreach (var layer in gameLayers.Reverse())
{
    if (!layer.WantsInputEvents) continue;
    layer.HandleInputEvent(inputEvent);
    if (inputEvent.IsHandled) return;
}
```

---

## Benefits Summary
- **No Singletons**: All dependencies properly injected
- **Thread Safety**: `ConcurrentQueue` eliminates need for locking
- **Performance**: Event filtering, batched processing
- **Testability**: Easy to mock input for unit tests
- **Maintainability**: Clear separation of concerns
- **Cross-Platform**: Platform code properly isolated
- **Event Optimization**: Layers only receive events they need

**Estimated Effort**: 2-3 days with comprehensive testing