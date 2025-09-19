namespace Engine.Events.Window;

public abstract record WindowEvent : Event
{
    protected WindowEvent(EventType eventType)
    {
        EventType = eventType;
    }

    protected override EventType EventType { get; }
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record WindowResizeEvent(int Width, int Height) : WindowEvent(EventType.WindowsResize);
public record WindowCloseEvent() : WindowEvent(EventType.WindowClose);
public record WindowFocusEvent() : WindowEvent(EventType.WindowFocus);
public record WindowLostFocusEvent() : WindowEvent(EventType.WindowLostFocus);
public record WindowMovedEvent() : WindowEvent(EventType.WindowMoved);