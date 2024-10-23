using Engine.Events;

namespace Engine.Core.Window;

public interface IGameWindow
{
    void Run();
    event Action<Event> OnEvent;
    event Action OnUpdate;
    event Action<WindowCloseEvent> OnClose;
    event Action OnWindowLoad;
}