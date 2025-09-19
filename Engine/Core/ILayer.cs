using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Core;

public interface ILayer
{
    void OnAttach(IInputSystem inputSystem);
    void OnDetach();
    void OnUpdate(TimeSpan timeSpan);
    void OnImGuiRender();
    void HandleInputEvent(InputEvent windowEvent);
    void HandleWindowEvent(WindowEvent windowEvent);
}
