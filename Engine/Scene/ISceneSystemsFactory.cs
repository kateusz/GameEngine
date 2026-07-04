using ECS;
using ECS.Systems;
using Engine.Scene.Systems;

namespace Engine.Scene;

public interface ISceneSystemsFactory
{
    void PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue);
}
