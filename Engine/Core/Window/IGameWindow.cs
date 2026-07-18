using System.Numerics;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Core.Window;

public interface IGameWindow : IContentScaleProvider
{
    void Run();

    /// <summary>Logical client size (matches mouse coordinates, not framebuffer pixels).</summary>
    Vector2 ClientSize { get; }

    event Action<WindowEvent> OnWindowEvent;  // Resize, close, focus, etc.
    event Action<InputEvent> OnInputEvent;    // Keys, mouse, gamepad, etc.
    event Action<double> OnUpdate;            // Receives platform-provided delta time in seconds
    event Action<WindowCloseEvent> OnClose;
    event Action<IInputSystem> OnWindowLoad;
}