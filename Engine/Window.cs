using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine;

public record WindowProps(string Title, int Width, int Height);

public interface IWindow
{
    int Width { get; }
    int Height { get; }
    void Run();
    //void OnUpdate();
    Action EvertCallback { get; }
}

public class Window : GameWindow, IWindow
{
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(
        gameWindowSettings, nativeWindowSettings)
    {
    }

    public Window(WindowProps props) : base(GameWindowSettings.Default,
        new NativeWindowSettings { Size = (props.Width, props.Height), Title = props.Title })
    {
    }

    public int Width { get; }
    public int Height { get; }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }
    
    protected override void OnLoad()
    {
        base.OnLoad();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
    }

    public Action EvertCallback { get; }
}