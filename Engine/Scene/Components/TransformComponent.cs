using System;
using System.Collections.Generic;
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

    private readonly List<int> _childIds = new();
    private int? _parentId;
    private Matrix4x4 _cachedWorldTransform;
    private bool _isWorldDirty = true;

    internal event Action? LocalChanged;

    public Vector3 Translation
    {
        get => _translation;
        set { _translation = value; _isDirty = true; _isWorldDirty = true; LocalChanged?.Invoke(); }
    }

    public Vector3 Rotation
    {
        get => _rotation;
        set { _rotation = value; _isDirty = true; _isWorldDirty = true; LocalChanged?.Invoke(); }
    }

    public Vector3 Scale
    {
        get => _scale;
        set { _scale = value; _isDirty = true; _isWorldDirty = true; LocalChanged?.Invoke(); }
    }

    public int? ParentId => _parentId;

    public IReadOnlyList<int> ChildIds => _childIds;

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

    public Matrix4x4 GetWorldTransform(Func<int, TransformComponent?> resolveParent)
    {
        if (!_isWorldDirty && !_isDirty)
            return _cachedWorldTransform;

        var local = GetTransform();

        if (_parentId is null)
        {
            _cachedWorldTransform = local;
        }
        else
        {
            var parent = resolveParent(_parentId.Value);
            _cachedWorldTransform = parent is null
                ? local
                : local * parent.GetWorldTransform(resolveParent);
        }

        _isWorldDirty = false;
        return _cachedWorldTransform;
    }

    internal bool IsWorldDirty => _isWorldDirty;

    internal void MarkWorldDirty() => _isWorldDirty = true;

    internal void SetParentIdInternal(int? parentId)
    {
        _parentId = parentId;
        _isWorldDirty = true;
    }

    internal void AddChildIdInternal(int childId)
    {
        if (!_childIds.Contains(childId))
            _childIds.Add(childId);
    }

    internal void RemoveChildIdInternal(int childId)
    {
        _childIds.Remove(childId);
    }

    public IComponent Clone()
    {
        return new TransformComponent(_translation, _rotation, _scale);
    }
}