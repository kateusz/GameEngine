using Engine.Events;

namespace Engine.Core.Window;

public interface IWindow
{
    void Run();
    void SwapBuffers();
    event Action<Event> OnEvent;
    event Action OnUpdate;
    event Action<WindowCloseEvent> OnClose;
}