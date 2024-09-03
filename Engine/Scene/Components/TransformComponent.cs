using System.Numerics;
using ECS;
using Engine.Math;

namespace Engine.Scene.Components;

public class TransformComponent : Component
{
    public Vector3 Translation { get; set; }
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;

    public TransformComponent()
    {
        Translation = Vector3.Zero;
    }
    
    public TransformComponent(Vector3 translation)
    {
        Translation = translation;
    }
    
    public Matrix4x4 GetTransform()
    {
        //var rotationX = Matrix4x4.CreateRotationX(Rotation.X);
        //var rotationY = Matrix4x4.CreateRotationY(Rotation.Y);
        //var rotationZ = Matrix4x4.CreateRotationZ(Rotation.Z);
        //var rotation = rotationX * rotationY * rotationZ;
        
        // Example Euler angles in radians
        var eulerAngles = new Vector3(0.5f, 1.0f, 0.3f);

        // Convert Euler angles to Quaternion
        var quaternion = MathHelpers.QuaternionFromEuler(eulerAngles);

        // Convert Quaternion to Matrix4x4
        var rotation = MathHelpers.MatrixFromQuaternion(quaternion);
        var translation = Matrix4x4.CreateTranslation(Translation);
        var scale = Matrix4x4.CreateScale(Scale);

        return translation * rotation * scale;
    }
}