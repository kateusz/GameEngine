using ECS.Systems;

namespace Engine.Scene;

public interface IPhysicsSimulationSystemFactory
{
    ISystem Create();
}
