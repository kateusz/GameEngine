using ECS;
using ECS.Systems;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

public interface ISceneSystemsFactory
{
    IPhysicsQueries PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue);
}
