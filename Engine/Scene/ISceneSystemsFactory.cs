using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

public interface ISceneSystemsFactory
{
    IPhysicsQueries PopulateSystemManager(
        ISystemManager systemManager,
        IContext context,
        PhysicsRuntimeBodyStore bodyStore,
        PhysicsRuntimeBodyStore3D? bodyStore3D,
        PhysicsContactQueue contactQueue,
        ScriptRuntimeStore scriptStore,
        SceneDimension dimension = SceneDimension.TwoD);
}
