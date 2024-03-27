using Engine.Core.Window;
using Engine.Events;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenGL;

public class OpenGLWindow : GameWindow, IWindow
{
    private readonly OpenGLContext _context;

    public OpenGLWindow(WindowProps props) : base(GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Size = (props.Width, props.Height),
            Title = props.Title,
            Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.On
        })
    {
        _context = new OpenGLContext();
        _context.Init(SwapBuffers);
    }

    public event Action<Event> OnEvent = null!;
    public event Action OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;

    public static KeyboardState Keyboard { get; private set; } = null!;
    public static MouseState Mouse { get; private set; } = null!;

    protected override void OnLoad()
    {
        Keyboard = KeyboardState;
        Mouse = MouseState;
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
        
        // TODO: refactor this
        // Update the opengl viewport
        //GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        
        var @event = new WindowResizeEvent(e.Width, e.Height);
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