using Engine.Core.Window;
using Engine.Events;
using OpenTK.Graphics.ES11;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenTK;

public class OpenTKGameWindow : GameWindow, IGameWindow
{
    private readonly OpenTKContext _context;
    private float _scaleFactorX;
    private float _scaleFactorY;

    public OpenTKGameWindow(WindowProps props) : base(GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Size = (props.Width, props.Height),
            Title = props.Title,
            Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.On
        })
    {
        _context = new OpenTKContext();
        _context.Init(SwapBuffers);
    }

    public event Action<Event> OnEvent = null!;
    public event Action OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;
    public event Action OnWindowLoad;

    public static KeyboardState Keyboard { get; private set; } = null!;
    public static MouseState Mouse { get; private set; } = null!;

    protected override void OnLoad()
    {
        // Initialise the Scale Factor
        _scaleFactorX = 1.0f;
        _scaleFactorY = 1.0f;

        // Get the Scale Factor of the Monitor
        TryGetCurrentMonitorScale(out _scaleFactorX, out _scaleFactorY);

        Keyboard = KeyboardState;
        Mouse = MouseState;

        OnWindowLoad();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        Keyboard = KeyboardState;
        Mouse = MouseState;

        OnUpdate();
        _context.SwapBuffers();

        if (!KeyboardState.IsKeyDown(Keys.Escape))
            return;

        Close();
        OnClose(new WindowCloseEvent());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        var width = (int)(Size.X * _scaleFactorX);
        var height = (int)(Size.Y * _scaleFactorY);

        GL.Viewport(0, 0, width, height);

        //var @event = new WindowResizeEvent(Size.X, Size.Y);
        var @event = new WindowResizeEvent(width, height);
        OnEvent(@event);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        var @event = new KeyPressedEvent((int)e.Key, 1);
        OnEvent(@event);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        var @event = new KeyReleasedEvent((int)e.Key);
        OnEvent(@event);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var @event = new MouseButtonReleasedEvent((int)e.Button);
        OnEvent(@event);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        var @event = new MouseButtonPressedEvent((int)e.Button);
        OnEvent(@event);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var @event = new MouseScrolledEvent(e.OffsetX, e.OffsetY);
        OnEvent(@event);
    }
}