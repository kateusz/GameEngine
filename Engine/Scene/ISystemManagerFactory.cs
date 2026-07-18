using ECS;
using ECS.Systems;
using Engine.Physics;
using Engine.Scene.Systems;

namespace Engine.Scene;

public sealed record SceneBuildResult(
    ISystemManager SystemManager,
    PhysicsRuntimeBodyStore BodyStore,
    PhysicsContactQueue ContactQueue,
    ScriptRuntimeStore ScriptStore,
    IPhysicsWorld2D PhysicsWorld);

public interface ISystemManagerFactory
{
    SceneBuildResult Create(IContext context);
}
