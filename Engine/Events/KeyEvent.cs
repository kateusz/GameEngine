namespace Engine.Events;

public abstract class KeyEvent : Event
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

public class KeyPressedEvent : KeyEvent
{
    public int RepeatCount { get; }

    public KeyPressedEvent(int keyCode, int repeatCount) : base(keyCode, EventType.KeyPressed)
    {
        RepeatCount = repeatCount;
    }
}

public class KeyReleasedEvent : KeyEvent
{
    public KeyReleasedEvent(int keyCode) : base(keyCode, EventType.KeyReleased)
    {
    }
}
