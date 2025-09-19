Comprehensive Refactoring Plan: Input System with Dependency Injection

Current Architecture Issues Identified:

1. Multiple Singleton Anti-patterns:
   - InputState.Instance (singleton)
   - SilkNetGameWindow.Keyboard/Mouse (static access)
   - KeyboardStateFactory.Create() (static factory)
2. Tight Coupling: SilkNetKeyboardState directly accesses SilkNetGameWindow.Keyboard
3. Mixed Responsibilities: SilkNetGameWindow handles both windowing AND input initialization
4. Missing DI Integration: Input system not registered with existing DryIoc container

  ---
Phase 1: Core Abstractions & Interfaces

Create new interfaces following game engine standards:

// Engine/Core/Input/IInputManager.cs
public interface IInputManager : IDisposable
{
IKeyboardState Keyboard { get; }
IMouseState Mouse { get; }
void Update(TimeSpan deltaTime);
event Action<InputEvent> InputReceived;
}

// Engine/Core/Input/IInputContext.cs  
public interface IInputContext : IDisposable
{
IKeyboardState Keyboard { get; }
IMouseState Mouse { get; }
void Initialize();
}

// Engine/Core/Input/IInputContextFactory.cs
public interface IInputContextFactory
{
IInputContext CreateContext(object platformContext);
}

  ---
Phase 2: Remove Static Dependencies

2.1 Refactor SilkNetGameWindow:
- Remove public static IKeyboard Keyboard { get; private set; }
- Remove public static IMouse Mouse { get; private set; }
- Inject IInputContextFactory via constructor
- Delegate input creation to injected factory

2.2 Refactor SilkNetKeyboardState:
// Before: SilkNetGameWindow.Keyboard.IsKeyPressed((Key)((int)keycode))
// After: Constructor injection of IKeyboard dependency
public class SilkNetKeyboardState : IKeyboardState
{
private readonly IKeyboard _keyboard;

      public SilkNetKeyboardState(IKeyboard keyboard)
      {
          _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
      }

      public bool IsKeyPressed(KeyCodes keycode)
      {
          return _keyboard.IsKeyPressed((Key)((int)keycode));
      }
}

  ---
Phase 3: Implement Input Manager

Create centralized input management:
// Engine/Core/Input/InputManager.cs
public class InputManager : IInputManager
{
private readonly IInputContext _inputContext;

      public InputManager(IInputContext inputContext)
      {
          _inputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
          _inputContext.Initialize();
      }

      public IKeyboardState Keyboard => _inputContext.Keyboard;
      public IMouseState Mouse => _inputContext.Mouse;

      public void Update(TimeSpan deltaTime)
      {
          // Handle input polling/events
      }

      public event Action<InputEvent>? InputReceived;

      public void Dispose() => _inputContext?.Dispose();
}

  ---
Phase 4: Platform-Specific Implementation

4.1 SilkNet Input Context:
// Engine/Platform/SilkNet/Input/SilkNetInputContext.cs
public class SilkNetInputContext : IInputContext
{
private readonly Silk.NET.Input.IInputContext _silkContext;
private IKeyboardState? _keyboard;
private IMouseState? _mouse;

      public SilkNetInputContext(Silk.NET.Input.IInputContext silkContext)
      {
          _silkContext = silkContext ?? throw new ArgumentNullException(nameof(silkContext));
      }

      public IKeyboardState Keyboard => _keyboard ?? throw new InvalidOperationException("Input not initialized");
      public IMouseState Mouse => _mouse ?? throw new InvalidOperationException("Input not initialized");

      public void Initialize()
      {
          var silkKeyboard = _silkContext.Keyboards[0];
          var silkMouse = _silkContext.Mice[0];

          _keyboard = new SilkNetKeyboardState(silkKeyboard);
          _mouse = new SilkNetMouseState(silkMouse);
      }

      public void Dispose()
      {
          _silkContext?.Dispose();
      }
}

4.2 SilkNet Factory:
// Engine/Platform/SilkNet/Input/SilkNetInputContextFactory.cs
public class SilkNetInputContextFactory : IInputContextFactory
{
public IInputContext CreateContext(object platformContext)
{
if (platformContext is not Silk.NET.Input.IInputContext silkContext)
throw new ArgumentException("Expected SilkNet IInputContext", nameof(platformContext));

          return new SilkNetInputContext(silkContext);
      }
}

  ---
Phase 5: DryIoc Registration

Update Program.cs files:
// Add to Editor/Program.cs, Sandbox/Program.cs, Benchmark/Program.cs
container.Register<IInputContextFactory, SilkNetInputContextFactory>(Reuse.Singleton);
container.Register<IInputManager, InputManager>(Reuse.Singleton);

// Modify GameWindow registration to inject dependencies:
container.Register<IGameWindow>(Reuse.Singleton,
made: Made.Of(() => new SilkNetGameWindow(
Arg.Of<IWindow>(),
Arg.Of<IInputContextFactory>())));

  ---
Phase 6: Application Integration

6.1 Remove Static Initialization:
// Engine/Core/Application.cs
// REMOVE: InputState.Init();
// ADD: Constructor injection
protected Application(IGameWindow gameWindow, IImGuiLayer imGuiLayer, IInputManager inputManager)
{
_gameWindow = gameWindow;
_inputManager = inputManager; // Store reference
// ... rest of constructor
}

6.2 Replace InputState.Instance Usage:
// Throughout codebase, replace:
// if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
//
// With injected dependency:
// if (_inputManager.Keyboard.IsKeyPressed(KeyCodes.A))

  ---
Phase 7: Gradual Migration Strategy

7.1 Backward Compatibility Bridge (Temporary):
// Engine/Core/Input/InputState.cs (Modified for transition)
public class InputState
{
private static IInputManager? _inputManager;

      public static void SetInputManager(IInputManager inputManager)
      {
          _inputManager = inputManager;
      }

      [Obsolete("Use dependency injection instead")]
      public static InputState Instance => new();

      [Obsolete("Use dependency injection instead")]
      public IKeyboardState Keyboard => _inputManager?.Keyboard ?? throw new InvalidOperationException();

      [Obsolete("Use dependency injection instead")]
      public IMouseState Mouse => _inputManager?.Mouse ?? throw new InvalidOperationException();
}

7.2 Migration Order:
1. Core interfaces (no breaking changes)
2. Platform implementations (no breaking changes)
3. DI registration (no breaking changes)
4. Application constructor (breaking change)
5. Update all callsites (breaking changes)
6. Remove deprecated code (cleanup)

  ---
Phase 8: Validation & Testing

8.1 Integration Points to Test:
- Window creation and input initialization
- Keyboard/mouse input in layers (EditorLayer, game scripts)
- Event propagation through layer stack
- Resource disposal on window close

8.2 Performance Validation:
- No additional allocations in hot paths
- Input polling performance maintained
- Event dispatch latency acceptable

  ---
Benefits Achieved:

✅ No Singletons: All dependencies injected through constructor✅ Separation of Concerns: Window ≠ Input Management✅ Testable Architecture: Easy to mock input for unit tests✅ Cross-Platform Ready: Platform-specific code
isolated✅ Resource Management: Proper disposal patterns✅ Game Engine Standards: Follows Unity/Unreal input patterns

  ---
Files to Modify:

New Files (7):
- Engine/Core/Input/IInputManager.cs
- Engine/Core/Input/IInputContext.cs
- Engine/Core/Input/IInputContextFactory.cs
- Engine/Core/Input/InputManager.cs
- Engine/Platform/SilkNet/Input/SilkNetInputContext.cs
- Engine/Platform/SilkNet/Input/SilkNetInputContextFactory.cs

Modified Files (8):
- Engine/Platform/SilkNet/SilkNetGameWindow.cs
- Engine/Platform/SilkNet/Input/SilkNetKeyboardState.cs
- Engine/Core/Application.cs
- Engine/Core/Input/InputState.cs (deprecated bridge)
- Editor/Program.cs
- Sandbox/Program.cs
- Benchmark/Program.cs
- All files using InputState.Instance.*

Estimated Effort: 2-3 days for complete migration with testing