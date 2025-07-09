using Engine.Core.Input;
using Engine.Core.Window;
using Engine.Events;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;

namespace Engine.Platform.SilkNet;

public class SilkNetGameWindow : IGameWindow
{
    private readonly IWindow _window;

    public SilkNetGameWindow(WindowProps props)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";

        _window = Window.Create(options);
        _window.WindowState = WindowState.Maximized;

        _window.Load += WindowOnLoad;
        _window.Update += WindowOnUpdate;
        _window.Closing += OnWindowClosing;
        _window.FramebufferResize += OnFrameBufferResize;
    }

    public event Action<Event> OnEvent = null!;
    public event Action OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;
    public event Action OnWindowLoad = null!;

    public static IKeyboard Keyboard { get; private set; } = null!;
    public static IMouse Mouse { get; private set; } = null!;

    public void Run()
    {
        _window.Run();
    }

    private void OnWindowClosing()
    {
        OnEvent(new WindowCloseEvent());
        OnClose(new WindowCloseEvent());

        // Dispose the input context
        SilkNetContext.InputContext.Dispose();

        // Unload OpenGL
        SilkNetContext.GL.Dispose();
    }
    
    private void WindowOnLoad()
    {
        SilkNetContext.GL = _window.CreateOpenGL();
        SilkNetContext.Window = _window;

        Console.WriteLine("Load!");

        SilkNetContext.InputContext = _window.CreateInput();
        
        var keyboard = SilkNetContext.InputContext.Keyboards[0];
        var mouse = SilkNetContext.InputContext.Mice[0];

        Mouse = mouse;
        Mouse.Scroll += OnMouseWheel;
        Mouse.MouseDown += OnMouseDown;
        
        Keyboard = keyboard;

        for (int i = 0; i < SilkNetContext.InputContext.Keyboards.Count; i++)
            SilkNetContext.InputContext.Keyboards[i].KeyDown += KeyDown;

        OnWindowLoad();
    }

    private void OnMouseDown(IMouse arg1, MouseButton arg2)
    {
        var @event = new MouseButtonPressedEvent((int)arg2);
        OnEvent(@event);
    }

    private void WindowOnUpdate(double deltaTime)
    {
        Mouse = SilkNetContext.InputContext.Mice[0];
        OnUpdate();

        if (!InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Escape))
            return;
        
        _window.Close();
    }

    private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
            _window.Close();

        OnEvent(new KeyPressedEvent((int)key, true));
    }

    private void OnFrameBufferResize(Vector2D<int> newSize)
    {
        //Update aspect ratios, clipping regions, viewports, etc.
        SilkNetContext.GL.Viewport(newSize);

        var @event = new WindowResizeEvent(newSize.X, newSize.Y);
        OnEvent(@event);
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        var @event = new MouseScrolledEvent(scrollWheel.X, scrollWheel.Y);
        OnEvent(@event);
    }
}