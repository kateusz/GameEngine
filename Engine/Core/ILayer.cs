using Engine.Events;

namespace Engine.Core;

public interface ILayer
{
    void OnAttach();
    void OnDetach();
    void OnUpdate(TimeSpan timeSpan);
    void OnImGuiRender();
    void HandleEvent(Event @event);
}
