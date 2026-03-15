using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Core.Window;

public interface IGameWindow
{
    void Run();

    /// <summary>
    /// Ratio of physical (framebuffer) pixels to logical (window) pixels.
    /// Returns 2.0 on macOS Retina, 1.0 on standard displays.
    /// </summary>
    float ContentScale { get; }

    event Action<WindowEvent> OnWindowEvent;  // Resize, close, focus, etc.
    event Action<InputEvent> OnInputEvent;    // Keys, mouse, gamepad, etc.
    event Action<double> OnUpdate;            // Receives platform-provided delta time in seconds
    event Action<WindowCloseEvent> OnClose;
    event Action<IInputSystem> OnWindowLoad;
}