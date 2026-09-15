using Engine.Events.Input;

namespace Engine.Input;

public interface IInputSystem : IDisposable
{
    void Update(TimeSpan deltaTime);
    event Action<InputEvent> InputReceived;
}
