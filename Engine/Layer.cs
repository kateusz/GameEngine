using Engine.Events;

namespace Engine;

public interface ILayer
{
    string Name { get; }
    void OnAttach();
    void OnDetach();
    void OnUpdate();
    void OnEvent(Event @event);
}

public class Layer : ILayer
{
    public string Name { get; }

    protected Layer(string name)
    {
        Name = name;
    }

    public virtual void OnAttach()
    {
    }

    public virtual void OnDetach()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnEvent(Event @event)
    {
    }
}