using Engine.Core.Window;
using Engine.Events;
using Engine.Platform.SilkNet;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;

namespace Engine.Platform.OpenGL;

public class SilkNetGameWindow : IGameWindow
{
    private readonly IWindow _window;
    
    private  IInputContext _inputContext;
    private float _scaleFactorX;
    private float _scaleFactorY;

    public SilkNetGameWindow(WindowProps props)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "My first Silk.NET program!";

        _window = Window.Create(options);
        
        _window.Load += WindowOnLoad;
        _window.Update += WindowOnUpdate;
        _window.Render += WindowOnRender;
        _window.FramebufferResize += OnFramebufferResize;
    }

    public void Run()
    {
        
        _window.Run();
    }

    public void SwapBuffers()
    {
        
    }

    public event Action<Event> OnEvent = null!;
    public event Action OnUpdate = null!;
    public event Action<WindowCloseEvent> OnClose = null!;
    public event Action OnWindowLoad = null!;

    public static IKeyboard Keyboard { get; private set; } = null!;
    public static IMouse Mouse { get; private set; } = null!;

    private void WindowOnLoad()
    {
        SilkNetContext.GL = _window.CreateOpenGL();
        
        // Initialise the Scale Factor
        _scaleFactorX = 1.0f;
        _scaleFactorY = 1.0f;
        
        Console.WriteLine("Load!");

        _inputContext = _window.CreateInput();
        for (int i = 0; i < _inputContext.Keyboards.Count; i++)
            _inputContext.Keyboards[i].KeyDown += KeyDown;
        
        OnWindowLoad();
    }

    private void WindowOnUpdate(double deltaTime)
    {
        var keyboard = _inputContext.Keyboards[0];
        var mouse = _inputContext.Mice[0];
        
        Mouse = mouse;
        Keyboard = keyboard;
        
        OnUpdate();
        //_context.SwapBuffers();

        // if (!KeyboardState.IsKeyDown(Keys.Escape))
        //     return;

        //Close();
        //OnClose(new WindowCloseEvent());
    }

    private void WindowOnRender(double deltaTime)
    {
        
    }
    
    private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
            _window.Close();
    }
    
    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        //Update aspect ratios, clipping regions, viewports, etc.
    }
    

    // protected override void OnResize(ResizeEventArgs e)
    // {
    //     base.OnResize(e);
    //     var width = (int)(Size.X * _scaleFactorX);
    //     var height = (int)(Size.Y * _scaleFactorY);
    //
    //     GL.Viewport(0, 0, width, height);
    //
    //     //var @event = new WindowResizeEvent(Size.X, Size.Y);
    //     var @event = new WindowResizeEvent(width, height);
    //     OnEvent(@event);
    // }
    //
    // protected override void OnKeyUp(KeyboardKeyEventArgs e)
    // {
    //     var @event = new KeyPressedEvent((int)e.Key, 1);
    //     OnEvent(@event);
    // }
    //
    // protected override void OnKeyDown(KeyboardKeyEventArgs e)
    // {
    //     var @event = new KeyReleasedEvent((int)e.Key);
    //     OnEvent(@event);
    // }
    //
    // protected override void OnMouseUp(MouseButtonEventArgs e)
    // {
    //     var @event = new MouseButtonReleasedEvent((int)e.Button);
    //     OnEvent(@event);
    // }
    //
    // protected override void OnMouseDown(MouseButtonEventArgs e)
    // {
    //     var @event = new MouseButtonPressedEvent((int)e.Button);
    //     OnEvent(@event);
    // }
    //
    // protected override void OnMouseWheel(MouseWheelEventArgs e)
    // {
    //     var @event = new MouseScrolledEvent(e.OffsetX, e.OffsetY);
    //     OnEvent(@event);
    // }
}