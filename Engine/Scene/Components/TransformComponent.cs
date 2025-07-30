using System.Numerics;
using ECS;
using Engine.Math;

namespace Engine.Scene.Components;

public record struct TransformComponent(Vector3 Translation, Vector3 Rotation, Vector3 Scale) : IComponent
{
    public TransformComponent() : this(Vector3.Zero, Vector3.Zero, Vector3.One) { }
    
    public Matrix4x4 GetTransform()
    {
        // Convert Euler angles to Quaternion
        var quaternion = MathHelpers.QuaternionFromEuler(Rotation);

        // Convert Quaternion to Matrix4x4
        var rotation = MathHelpers.MatrixFromQuaternion(quaternion);
        var translation = Matrix4x4.CreateTranslation(Translation);
        var scale = Matrix4x4.CreateScale(Scale);

        return translation * rotation * scale;
    }
}