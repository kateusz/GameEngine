using Engine.Events.Input;
using Silk.NET.Input;

namespace Engine.Core.Input;

public interface IInputSystem : IDisposable
{
    IInputContext Context { get; set; }
    void Update(TimeSpan deltaTime);
    event Action<InputEvent> InputReceived;
}