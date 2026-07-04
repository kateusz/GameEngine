using ECS.Systems;

namespace Engine.Scene.Systems;

public class LightingSystem : ISystem
{
    public int Priority { get; }
    public void OnInit()
    {
        throw new NotImplementedException();
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown()
    {
        throw new NotImplementedException();
    }
}