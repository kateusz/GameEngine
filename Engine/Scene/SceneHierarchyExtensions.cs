using System.Numerics;
using ECS;
using Engine.Scene.Components;

namespace Engine.Scene;

public static class SceneHierarchyExtensions
{
    public static Matrix4x4 GetWorldTransform(this Entity entity, IContext context)
    {
        var t = entity.GetComponent<TransformComponent>();
        return t.GetWorldTransform(id =>
        {
            if (!context.TryGetById(id, out var parent))
                return null;
            return parent.TryGetComponent<TransformComponent>(out var pt) ? pt : null;
        });
    }
}
