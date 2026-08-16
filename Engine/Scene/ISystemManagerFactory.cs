using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

public sealed record SceneBuildResult(
    ISystemManager SystemManager,
    PhysicsRuntimeBodyStore BodyStore,
    PhysicsContactQueue ContactQueue,
    ScriptRuntimeStore ScriptStore,
    IPhysicsQueries PhysicsQueries);

public interface ISystemManagerFactory
{
    SceneBuildResult Create(IContext context, SceneDimension dimension = SceneDimension.TwoD);
}
