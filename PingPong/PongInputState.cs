namespace PingPong;

internal sealed class PongInputState : IPongInputState
{
    public bool MoveUpPressed { get; set; }
    public bool MoveDownPressed { get; set; }
}
