namespace Engine.Events;

public class MouseMovedEvent : Event
{
    public MouseMovedEvent(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public uint X { get; }
    public uint Y { get; }

    protected override EventType EventType => EventType.MouseMoved;

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public class MouseScrolledEvent : Event
{
    public MouseScrolledEvent(float xOffSet, float yOffset)
    {
        XOffSet = xOffSet;
        YOffset = yOffset;
    }

    public float XOffSet { get; }
    public float YOffset { get; }

    protected override EventType EventType => EventType.MouseScrolled;

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public abstract class MouseButtonEvent : Event
{
    public int Button { get; set; }

    protected MouseButtonEvent(int button)
    {
        Button = button;
    }
    
    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public class MouseButtonPressedEvent : MouseButtonEvent
{
    public MouseButtonPressedEvent(int button) : base(button)
    {
        
    }
    protected override EventType EventType => EventType.MouseButtonPressed;
}

public class MouseButtonReleasedEvent : MouseButtonEvent
{
    public MouseButtonReleasedEvent(int button) : base(button)
    {
        
    }
    protected override EventType EventType => EventType.MouseButtonReleased;
}