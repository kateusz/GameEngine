namespace Engine.Events;

public class WindowResizeEvent : Event
{
    public WindowResizeEvent(uint width, uint height)
    {
        Width = width;
        Height = height;
    }
    public uint Width { get; }
    public uint Height { get; }

    protected override EventType EventType => EventType.WindowsResize;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class WindowCloseEvent : Event
{
    protected override EventType EventType => EventType.WindowClose;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class WindowFocusEvent : Event
{
    protected override EventType EventType => EventType.WindowFocus;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class WindowLostFocusEvent : Event
{
    protected override EventType EventType => EventType.WindowLostFocus;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class WindowMovedEvent : Event
{
    protected override EventType EventType => EventType.WindowMoved;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class AppTickEvent : Event
{
    protected override EventType EventType => EventType.AppTick;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class AppUpdateEvent : Event
{
    protected override EventType EventType => EventType.AppUpdate;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}

public class AppRenderEvent : Event
{
    protected override EventType EventType => EventType.AppRender;
    protected override EventCategory EventCategory => EventCategory.EventCategoryApplication;
}