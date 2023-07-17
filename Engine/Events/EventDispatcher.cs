namespace Engine.Events;

public class EventDispatcher<TEvent> where TEvent : Event
{
    private readonly TEvent _event;

    public EventDispatcher(TEvent @event)
    {
        _event = @event;
    }

    public bool Dispatch(Func<TEvent, bool> func)
    {
        try
        {
            _event.IsHandled = func(_event);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}