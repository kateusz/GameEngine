namespace Engine.Events.Input;

public abstract record MouseEvent : InputEvent
{
    protected MouseEvent(EventType eventType)
    {
        EventType = eventType;
    }

    protected override EventType EventType { get; }

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public record MouseMovedEvent(uint X, uint Y) : MouseEvent(EventType.MouseMoved);

public record MouseScrolledEvent(float XOffSet, float YOffset) : MouseEvent(EventType.MouseScrolled);

public abstract record MouseButtonEvent(int Button, EventType EventType) : MouseEvent(EventType)
{
    public int Button { get; set; } = Button;
}

public record MouseButtonPressedEvent(int Button) : MouseButtonEvent(Button, EventType.MouseButtonPressed);

public record MouseButtonReleasedEvent(int Button) : MouseButtonEvent(Button, EventType.MouseButtonReleased);