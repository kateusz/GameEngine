using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Platform.SilkNet.Input;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

public class SilkNetGameWindow : IGameWindow
{
    private readonly IWindow _window;
    
    private IInputSystem? _inputSystem;

    public SilkNetGameWindow(IWindow window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        
        _window.WindowState = WindowState.Maximized;

        _window.Load += WindowOnLoad;
        _window.Update += WindowOnUpdate;
        _window.Closing += OnWindowClosing;
        _window.FramebufferResize += OnFrameBufferResize;
    }

    public event Action<Event> OnEvent = null!;
    public event Action<InputEvent> OnInputEvent;
    public event Action OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;
    public event Action<IInputSystem> OnWindowLoad = null!;

    public void Run()
    {
        _window.Run();
    }

    public event Action<WindowEvent>? OnWindowEvent;

    private void OnWindowClosing()
    {
        OnEvent(new WindowCloseEvent());
        OnClose(new WindowCloseEvent());

        // Unload OpenGL
        SilkNetContext.GL.Dispose();
    }
    
    private void WindowOnLoad()
    {
        SilkNetContext.GL = _window.CreateOpenGL();
        SilkNetContext.Window = _window;

        Console.WriteLine("Load!");
        
        var inputContext = _window.CreateInput();
        // TODO: move to factory
        _inputSystem = new SilkNetInputSystem(inputContext);
        _inputSystem.InputReceived += OnInputReceived;
        
        OnWindowLoad(_inputSystem);
    }

    private void WindowOnUpdate(double deltaTime)
    {
        OnUpdate();
    }
    
    private void OnInputReceived(InputEvent inputEvent)
    {
        OnInputEvent(inputEvent);
    }

    private void OnFrameBufferResize(Vector2D<int> newSize)
    {
        //Update aspect ratios, clipping regions, viewports, etc.
        SilkNetContext.GL.Viewport(newSize);

        var @event = new WindowResizeEvent(newSize.X, newSize.Y);
        OnEvent(@event);
    }
}