namespace Engine.Events.Input;

public record MouseMovedEvent(float X, float Y) : InputEvent;
public record MouseScrolledEvent(float XOffSet, float YOffset) : InputEvent;
public record MouseButtonPressedEvent(int Button) : InputEvent;
public record MouseButtonReleasedEvent(int Button) : InputEvent;
