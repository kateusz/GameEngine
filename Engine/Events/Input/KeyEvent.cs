using Engine.Core.Input;

namespace Engine.Events.Input;

public abstract record KeyEvent : InputEvent
{
    public KeyCodes KeyCode { get; }

    protected KeyEvent(KeyCodes keyCode, EventType eventType)
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

    public KeyPressedEvent(KeyCodes keyCode, bool isRepeat) : base(keyCode, EventType.KeyPressed)
    {
        IsRepeat = isRepeat;
    }
}

public record KeyReleasedEvent : KeyEvent
{
    public KeyReleasedEvent(KeyCodes keyCode) : base(keyCode, EventType.KeyReleased)
    {
    }
}
