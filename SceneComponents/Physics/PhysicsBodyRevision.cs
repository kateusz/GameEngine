using ECS;

namespace SceneComponents.Physics;

/// <summary>Monotonic revision bumped when rigid-body shape data changes.</summary>
internal static class PhysicsBodyRevision
{
    private static uint _value;

    internal static uint Value => _value;

    internal static void Bump() => _value++;

    static PhysicsBodyRevision()
    {
        Context.ComponentIndexed += OnComponentIndexed;
    }

    private static void OnComponentIndexed(Type componentType)
    {
        if (componentType == typeof(RigidBody2DComponent)
            || componentType == typeof(BoxCollider2DComponent)
            || componentType == typeof(CircleCollider2DComponent)
            || componentType == typeof(EdgeCollider2DComponent))
            Bump();
    }
}
