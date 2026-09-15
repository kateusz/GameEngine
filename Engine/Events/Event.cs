namespace Engine.Events;

public abstract record Event
{
    public bool IsHandled { get; set; }
}
