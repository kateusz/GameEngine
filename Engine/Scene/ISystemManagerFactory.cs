using ECS;
using ECS.Systems;
using Engine.Scene.Systems;

namespace Engine.Scene;

public sealed record SceneBuildResult(ISystemManager SystemManager, PhysicsRuntimeBodyStore BodyStore);

public interface ISystemManagerFactory
{
    SceneBuildResult Create(IContext context);
}
