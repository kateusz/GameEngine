using System.Numerics;
using ECS;
using Engine.Math;

namespace Engine.Scene.Components;

public struct TransformComponent : IComponent
{
    public Vector3 Translation { get; set; }
    
    /// <summary>
    /// Rotation in radians
    /// </summary>
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; }

    public TransformComponent()
    {
        Translation = Vector3.Zero;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
    }
    
    public TransformComponent(Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        Translation = translation;
        Rotation = rotation;
        Scale = scale;
    }
    
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