using Engine.Events;
using Silk.NET.OpenGL;

namespace Engine.Core.Window;

public interface IGameWindow
{
    void Run();
    void SwapBuffers();
    event Action<Event> OnEvent;
    event Action OnUpdate;
    event Action<WindowCloseEvent> OnClose;
    event Action OnWindowLoad;
}