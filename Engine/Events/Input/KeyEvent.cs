namespace Engine.Events.Input;

public abstract record KeyEvent : InputEvent
{
    public int KeyCode { get; }

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
    public bool IsRepeat { get; }

    public KeyPressedEvent(int keyCode, bool isRepeat) : base(keyCode, EventType.KeyPressed)
    {
        IsRepeat = isRepeat;
    }
}

public record KeyReleasedEvent : KeyEvent
{
    public KeyReleasedEvent(int keyCode) : base(keyCode, EventType.KeyReleased)
    {
    }
}
