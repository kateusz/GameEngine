using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;

namespace Engine.Scene;

public interface ISceneSystemsFactory
{
    IPhysicsWorld2D PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsContactQueue contactQueue,
        ScriptRuntimeStore scriptStore);
}
