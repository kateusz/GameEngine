namespace Engine.Scene.Systems.Pong;

public interface IPongInputState
{
    bool MoveUpPressed { get; set; }
    bool MoveDownPressed { get; set; }
}
