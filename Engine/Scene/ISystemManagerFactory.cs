using ECS;
using ECS.Systems;
using Engine.Scene.Systems;

namespace Engine.Scene;

public sealed record SceneBuildResult(
    ISystemManager SystemManager,
    PhysicsRuntimeBodyStore BodyStore,
    PhysicsContactQueue ContactQueue,
    ScriptRuntimeStore ScriptStore);

public interface ISystemManagerFactory
{
    SceneBuildResult Create(IContext context);
}
