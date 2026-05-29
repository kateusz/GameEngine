using Engine.Scene.Systems;

namespace Engine.Scene;

public class PhysicsSimulationSystemFactory
{
    public PhysicsSimulationSystem Create()
    {
        return new PhysicsSimulationSystem()
    }
}