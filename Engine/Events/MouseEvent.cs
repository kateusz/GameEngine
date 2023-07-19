namespace Engine.Events;

public record MouseMovedEvent(uint X, uint Y) : Event
{
    protected override EventType EventType => EventType.MouseMoved;

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public record MouseScrolledEvent(float XOffSet, float YOffset) : Event
{
    protected override EventType EventType => EventType.MouseScrolled;

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public abstract record MouseButtonEvent(int Button) : Event
{
    public int Button { get; set; } = Button;

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryMouse | EventCategory.EventCategoryInput;
}

public record MouseButtonPressedEvent(int Button) : MouseButtonEvent(Button)
{
    protected override EventType EventType => EventType.MouseButtonPressed;
}

public record MouseButtonReleasedEvent(int Button) : MouseButtonEvent(Button)
{
    protected override EventType EventType => EventType.MouseButtonReleased;
}