using Engine.Events;

namespace Engine;

public interface ILayer
{
    string Name { get; }
    event Action OnAttach;
    event Action OnDetach;
    event Action OnUpdate;
   // Action<Event> OnHandleEvent { get; }
   void HandleEvent(Event @event);
}

public class Layer : ILayer
{
    public string Name { get; }
    public event Action? OnAttach;
    public event Action? OnDetach;
    public event Action? OnUpdate;

    protected Layer(string name)
    {
        Name = name;
    }

    public virtual void HandleEvent(Event @event)
    {
    }
}