using Engine.Core;
using Engine.Events;
using Engine.Platform.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using IGraphicsContext = Engine.Renderer.IGraphicsContext;

namespace Engine;

public record WindowProps(string Title, int Width, int Height);

public interface IWindow
{
    void Run();
    event Action<Event> OnEvent;
    event Action OnUpdate;
    event Action<WindowCloseEvent> OnClose;
}

public class Window : GameWindow, IWindow
{
    private readonly IGraphicsContext _context;

    public Window(WindowProps props) : base(GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Size = (props.Width, props.Height),
            Title = props.Title,
            Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.On
        })
    {
        _context = new OpenGLContext(this);
        _context.Init();
    }

    public event Action<Event> OnEvent;
    public event Action OnUpdate;

    public event Action<WindowCloseEvent> OnClose;

    protected override void OnLoad()
    {
        
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        Input.KeyboardState = KeyboardState;
        Input.MouseState = MouseState;
    }

    // TODO: this is only needed for handling keyboard state?
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        // TODO: global static?
        Input.KeyboardState = KeyboardState;
        Input.MouseState = MouseState;
        
        OnUpdate();
        SwapBuffers();
        
        if (!KeyboardState.IsKeyDown(Keys.Escape)) 
            return;
        
        Close();
        OnClose(new WindowCloseEvent());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
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