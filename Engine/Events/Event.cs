namespace Engine.Events;

public abstract record Event
{
    public bool IsHandled { get; set; }
    protected abstract EventType EventType { get; }
    protected abstract EventCategory EventCategory { get; }

    protected bool IsInCategory(EventCategory category) => EventCategory.HasFlag(category);
}