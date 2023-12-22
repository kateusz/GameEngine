using Engine.Events;

namespace Engine.Core.Window;

public interface IWindow
{
    void Run();
    event Action<Event> OnEvent;
    event Action OnUpdate;
    event Action<WindowCloseEvent> OnClose;
}