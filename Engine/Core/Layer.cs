using Engine.Events;

namespace Engine.Core;

public interface ILayer
{
    string Name { get; }
    void OnAttach();
    void OnDetach();
    void OnUpdate(TimeSpan timeSpan);
    void OnImGuiRender();
    void HandleEvent(Event @event);
}

public abstract class Layer : ILayer
{
    public string Name { get; }

    protected Layer(string name)
    {
        Name = name;
    }
    
    public virtual void OnAttach(){}
    public virtual void OnDetach(){}

    public abstract void OnUpdate(TimeSpan timeSpan);

    public abstract void OnImGuiRender();

    public virtual void HandleEvent(Event @event)
    {
        @event.IsHandled = true;
    }
}