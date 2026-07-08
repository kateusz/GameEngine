using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Input;

namespace Engine.Platform.SilkNet;

internal sealed class SilkNetGameWindow(
    IWindow window,
    IInputSystemFactory inputSystemFactory,
    IGraphicsContext graphicsContext) : IGameWindow
{
    private static readonly ILogger Logger = Log.ForContext<SilkNetGameWindow>();

    public float ContentScale
    {
        get
        {
            if (window.Size.X == 0) return 1.0f;
            return (float)window.FramebufferSize.X / window.Size.X;
        }
    }

    public event Action<InputEvent> OnInputEvent;
    public event Action<double> OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;
    public event Action<IInputSystem> OnWindowLoad = null!;

    public void Run()
    {
        window.WindowState = WindowState.Maximized;

        window.Load += WindowOnLoad;
        window.Update += WindowOnUpdate;
        window.Closing += OnWindowClosing;
        window.FramebufferResize += OnFrameBufferResize;

        window.Run();
    }

    public event Action<WindowEvent>? OnWindowEvent;

    private void OnWindowClosing()
    {
        OnClose(new WindowCloseEvent());

        graphicsContext.Dispose();
    }

    private void WindowOnLoad()
    {
        graphicsContext.Create(window);
        SilkNetContext.Window = window;

        Logger.Information("SilkNet window loaded");

        var inputContext = window.CreateInput();

        // Create input system using factory (DI-based) instead of 'new'
        var inputSystem = inputSystemFactory.Create(inputContext);
        inputSystem.InputReceived += OnInputReceived;

        OnWindowLoad(inputSystem);

        var framebufferSize = window.FramebufferSize;
        OnFrameBufferResize(framebufferSize);
        Logger.Information("Initial framebuffer size: {Width}x{Height}", framebufferSize.X, framebufferSize.Y);
    }

    private void WindowOnUpdate(double deltaTime)
    {
        OnUpdate(deltaTime);
    }

    private void OnInputReceived(InputEvent inputEvent)
    {
        if (inputEvent is KeyPressedEvent { KeyCode: KeyCodes.Escape })
        {
            window.Close();
        }

        OnInputEvent(inputEvent);
    }

    private void OnFrameBufferResize(Vector2D<int> newSize)
    {
        Logger.Information("OnFrameBufferResize called, setting OpenGL viewport to: {Width}x{Height}", newSize.X, newSize.Y);

        //Update aspect ratios, clipping regions, viewports, etc.
        SilkNetContext.GL.Viewport(newSize);

        var @event = new WindowResizeEvent(newSize.X, newSize.Y);
        OnWindowEvent(@event);

        Logger.Information("WindowResizeEvent fired: {Width}x{Height}", newSize.X, newSize.Y);
    }
}
