using System.Numerics;

namespace Engine.Scene;

public struct TransformComponent
{
    public Matrix4x4 Transform;

    public TransformComponent(Matrix4x4 transform)
    {
        Transform = transform;
    }

    public static implicit operator Matrix4x4(TransformComponent transformComponent)
    {
        return transformComponent.Transform;
    }

    public static implicit operator TransformComponent(Matrix4x4 matrix)
    {
        return new TransformComponent(matrix);
    }
}