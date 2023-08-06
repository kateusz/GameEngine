using Engine.Events;

namespace Engine;

public interface ILayer
{
    string Name { get; }
    event Action OnAttach;
    event Action OnDetach;

    void OnUpdate();
    void HandleEvent(Event @event);
}

public class Layer : ILayer
{
    public string Name { get; }
    public event Action? OnAttach;
    public event Action? OnDetach;

    protected Layer(string name)
    {
        Name = name;
    }

    public virtual void OnUpdate()
    {
        
    }

    public virtual void HandleEvent(Event @event)
    {
    }
}