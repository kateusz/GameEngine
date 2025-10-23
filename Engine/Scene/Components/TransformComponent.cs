using System.Numerics;
using ECS;
using Engine.Math;

namespace Engine.Scene.Components;

public class TransformComponent : IComponent
{
    private Vector3 _translation;
    private Vector3 _rotation;
    private Vector3 _scale;

    private Matrix4x4 _cachedTransform;
    private bool _isDirty = true;

    public Vector3 Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            _isDirty = true;
        }
    }

    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _isDirty = true;
        }
    }

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _isDirty = true;
        }
    }

    public TransformComponent()
    {
        _translation = Vector3.Zero;
        _rotation = Vector3.Zero;
        _scale = Vector3.One;
    }

    public TransformComponent(Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        _translation = translation;
        _rotation = rotation;
        _scale = scale;
    }

    public Matrix4x4 GetTransform()
    {
        if (_isDirty)
        {
            // Convert Euler angles to Quaternion
            var quaternion = MathHelpers.QuaternionFromEuler(_rotation);

            // Convert Quaternion to Matrix4x4
            var rotation = MathHelpers.MatrixFromQuaternion(quaternion);
            var translation = Matrix4x4.CreateTranslation(_translation);
            var scale = Matrix4x4.CreateScale(_scale);

            _cachedTransform = translation * rotation * scale;
            _isDirty = false;
        }

        return _cachedTransform;
    }
}