using System.Numerics;
using ECS;
using Math;

namespace SceneComponents;

public class TransformComponent : IComponent
{
    private Vector3 _translation;
    private Vector3 _rotation;
    private Vector3 _scale;

    private Matrix4x4 _cachedTransform;
    private bool _isDirty = true;

    private Matrix4x4 _cachedWorldTransform = Matrix4x4.Identity;

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

    /// <summary>Local TRS matrix (relative to parent, or world if root).</summary>
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
            _cachedTransform = scale * rotation * translation;
            _isDirty = false;
        }

        return _cachedTransform;
    }

    /// <summary>Cached world matrix. Written by UpdateWorldTransforms.</summary>
    public Matrix4x4 GetWorldTransform() => _cachedWorldTransform;

    public void SetWorldTransform(Matrix4x4 world) => _cachedWorldTransform = world;

    public IComponent Clone()
    {
        return new TransformComponent(_translation, _rotation, _scale);
    }
}
