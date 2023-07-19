namespace Engine.Events;

public abstract record KeyEvent : Event
{
    protected int KeyCode { get; }

    protected KeyEvent(int keyCode, EventType eventType)
    {
        KeyCode = keyCode;
        EventType = eventType;
    }

    protected override EventType EventType { get; }

    protected override EventCategory EventCategory =>
        EventCategory.EventCategoryKeyboard | EventCategory.EventCategoryInput;
}

public record KeyPressedEvent : KeyEvent
{
    public int RepeatCount { get; }

    public KeyPressedEvent(int keyCode, int repeatCount) : base(keyCode, EventType.KeyPressed)
    {
        RepeatCount = repeatCount;
    }
}

public record KeyReleasedEvent : KeyEvent
{
    public KeyReleasedEvent(int keyCode) : base(keyCode, EventType.KeyReleased)
    {
    }
}
