using Engine.Events;
using Engine.Platform.OpenGL;
using OpenTK.Graphics.OpenGL4;
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
    void OnUpdate();
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
        })
    {
        _context = new OpenGLContext(this);
        _context.Init();
        
        
    }

    public event Action<Event> OnEvent;

    public void OnUpdate()
    {
        SwapBuffers();
    }

    public event Action<WindowCloseEvent> OnClose;

    protected override void OnLoad()
    {
        
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        var input = KeyboardState;
        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
        }
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
}