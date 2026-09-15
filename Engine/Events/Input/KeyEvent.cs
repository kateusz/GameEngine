using Input;

namespace Engine.Events.Input;

public record KeyPressedEvent(KeyCodes KeyCode, bool IsRepeat) : InputEvent;
public record KeyReleasedEvent(KeyCodes KeyCode) : InputEvent;
