using Engine.Events;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine;

public record WindowProps(string Title, int Width, int Height, Action<Event> OnEvent);

public interface IWindow
{
    void Run();
    event Action<Event> OnEvent;
    void OnUpdate();
}

public class Window : GameWindow, IWindow
{
    public Window(WindowProps props) : base(GameWindowSettings.Default,
        new NativeWindowSettings { Size = (props.Width, props.Height), Title = props.Title })
    {
        OnEvent = props.OnEvent;
    }
    
    public event Action<Event> OnEvent;
    public void OnUpdate()
    {
        
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        var @event = new WindowResizeEvent(e.Width, e.Height);
        OnEvent(@event);
    }
    
    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        
        var @event = new KeyPressedEvent((int)e.Key, 1);
        OnEvent(@event);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        var @event = new KeyReleasedEvent((int)e.Key);
        OnEvent(@event);
    }
    
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        var @event = new MouseButtonReleasedEvent((int)e.Button);
        OnEvent(@event);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        
        var @event = new MouseButtonPressedEvent((int)e.Button);
        OnEvent(@event);
    }
}