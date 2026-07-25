using ECS.Systems;
using Engine.Core;

namespace Engine.Scene.Systems;

/// <summary>
/// Propagates local transforms to world caches after physics/scripts, before audio/render.
/// </summary>
[SkipUnitTests]
internal sealed class TransformHierarchySystem(Action updateWorldTransforms) : ISystem
{
    public int Priority => SystemPriorities.TransformHierarchySystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime) => updateWorldTransforms();

    public void OnShutdown() { }
}
