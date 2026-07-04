using ECS.Systems;

namespace Engine.Scene;

internal sealed class SystemManagerFactory(
    ISceneSystemRegistry sceneSystemRegistry,
    IPhysicsSimulationSystemFactory physicsSimulationSystemFactory) : ISystemManagerFactory
{
    public ISystemManager Create()
    {
        var systemManager = new SystemManager();
        sceneSystemRegistry.PopulateSystemManager(systemManager);
        systemManager.RegisterSystem(physicsSimulationSystemFactory.Create());
        return systemManager;
    }
}
