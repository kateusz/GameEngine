namespace Engine.Events;

public record WindowResizeEvent(int Width, int Height) : Event
{
    protected override EventType EventType => EventType.WindowsResize;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record WindowCloseEvent : Event
{
    protected override EventType EventType => EventType.WindowClose;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record WindowFocusEvent : Event
{
    protected override EventType EventType => EventType.WindowFocus;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record WindowLostFocusEvent : Event
{
    protected override EventType EventType => EventType.WindowLostFocus;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record WindowMovedEvent : Event
{
    protected override EventType EventType => EventType.WindowMoved;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record AppTickEvent : Event
{
    protected override EventType EventType => EventType.AppTick;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record AppUpdateEvent : Event
{
    protected override EventType EventType => EventType.AppUpdate;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public record AppRenderEvent : Event
{
    protected override EventType EventType => EventType.AppRender;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}