using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Core.Window;

public interface IGameWindow
{
    void Run();
    event Action<WindowEvent> OnWindowEvent;  // Resize, close, focus, etc.
    event Action<InputEvent> OnInputEvent;    // Keys, mouse, gamepad, etc.
    event Action<double> OnUpdate;            // Receives platform-provided delta time in seconds
    event Action<WindowCloseEvent> OnClose;
    event Action<IInputSystem> OnWindowLoad;
}