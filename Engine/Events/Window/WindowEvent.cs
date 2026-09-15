namespace Engine.Events.Window;

public abstract record WindowEvent : Event;

public record WindowResizeEvent(int Width, int Height) : WindowEvent;
public record WindowCloseEvent() : WindowEvent;
