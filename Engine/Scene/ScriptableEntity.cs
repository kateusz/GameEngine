using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using ECS;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

/// <summary>
/// Base class for all script components in the engine.
/// </summary>
public abstract class ScriptableEntity
{
    private ISceneContext? _sceneContext;

    #region Reflection Cache

    /// <summary>
    /// Thread-safe cache for reflected field metadata to avoid repeated reflection operations.
    /// Keyed by script type, stores all public instance fields for that type.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> _fieldCache = new();

    /// <summary>
    /// Thread-safe cache for reflected property metadata to avoid repeated reflection operations.
    /// Keyed by script type, stores all public instance properties with getters and setters.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    #endregion

    /// <summary>
    /// The entity this script is attached to
    /// </summary>
    public Entity? Entity { get; private set; }

    internal void SetEntity(Entity entity) => Entity = entity;
    internal void SetSceneContext(ISceneContext ctx) => _sceneContext = ctx;

    #region Lifecycle Methods

    /// <summary>
    /// Called when the entity with this script is created or enabled.
    /// Use this to initialize components and set up references to other entities.
    /// </summary>
    public virtual void OnCreate()
    {
    }

    /// <summary>
    /// Called every frame during the update loop.
    /// </summary>
    /// <param name="ts">Time since the last frame</param>
    public virtual void OnUpdate(TimeSpan ts)
    {
    }

    /// <summary>
    /// Called when the entity with this script is destroyed or disabled.
    /// Use this for cleanup tasks (releasing resources, removing event listeners).
    /// </summary>
    public virtual void OnDestroy()
    {
    }

    #endregion

    #region Input Event Methods

    public virtual void OnKeyPressed(KeyCodes key)
    {
    }

    public virtual void OnKeyReleased(KeyCodes keyCode)
    {
    }

    public virtual void OnMouseButtonPressed(int button)
    {
    }

    #endregion

    #region Physics Event Methods

    /// <summary>
    /// Called when this entity begins colliding with another entity.
    /// </summary>
    /// <param name="other">The other entity involved in the collision</param>
    public virtual void OnCollisionBegin(Entity other)
    {
    }

    /// <summary>
    /// Called when this entity ends colliding with another entity.
    /// </summary>
    /// <param name="other">The other entity involved in the collision</param>
    public virtual void OnCollisionEnd(Entity other)
    {
    }

    /// <summary>
    /// Called when this entity enters a trigger area.
    /// </summary>
    /// <param name="other">The entity with the trigger collider</param>
    public virtual void OnTriggerEnter(Entity other)
    {
    }

    /// <summary>
    /// Called when this entity exits a trigger area.
    /// </summary>
    /// <param name="other">The entity with the trigger collider</param>
    public virtual void OnTriggerExit(Entity other)
    {
    }

    #endregion

    #region Component Utility Methods

    protected T GetComponent<T>() where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException("Entity is not set!");
        return Entity.GetComponent<T>();
    }

    protected bool HasComponent<T>() where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException("Entity is not set!");
        return Entity.HasComponent<T>();
    }

    protected T AddComponent<T>() where T : IComponent, new()
    {
        if (Entity == null)
            throw new InvalidOperationException("Entity is not set!");
        return Entity.AddComponent<T>();
    }

    protected void AddComponent<T>(T component) where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException("Entity is not set!");
        Entity.AddComponent<T>(component);
    }

    protected void RemoveComponent<T>() where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException("Entity is not set!");
        Entity.RemoveComponent<T>();
    }

    #endregion

    #region Entity Utility Methods

    protected Entity? FindEntity(string name)
    {
        var currentScene = _sceneContext?.ActiveScene;
        if (currentScene == null)
            return null;

        foreach (var entity in currentScene.Entities)
        {
            if (entity.Name == name)
                return entity;
        }

        return null;
    }

    protected Entity CreateEntity(string name)
    {
        var currentScene = _sceneContext?.ActiveScene;
        return currentScene?.CreateEntity(name)!;
    }

    protected void DestroyEntity(Entity entity)
    {
        var currentScene = _sceneContext?.ActiveScene;
        currentScene?.DestroyEntity(entity);
    }

    #endregion

    #region Transform Utility Methods

    protected Vector3 GetPosition() => !HasComponent<TransformComponent>()
        ? Vector3.Zero
        : GetComponent<TransformComponent>().Translation;

    protected void SetPosition(Vector3 position)
    {
        if (!HasComponent<TransformComponent>())
            return;

        var transform = GetComponent<TransformComponent>();
        transform.Translation = position;
        AddComponent(transform);
    }

    /// <summary>
    /// Get the rotation of this entity.
    /// </summary>
    /// <returns>The rotation in radians as a Vector3 (XYZ = pitch, yaw, roll)</returns>
    protected Vector3 GetRotation() => !HasComponent<TransformComponent>()
        ? Vector3.Zero
        : GetComponent<TransformComponent>().Rotation;

    protected void SetRotation(Vector3 rotation)
    {
        if (!HasComponent<TransformComponent>())
            return;

        var transform = GetComponent<TransformComponent>();
        transform.Rotation = rotation;
        AddComponent(transform);
    }

    protected Vector3 GetScale() =>
        !HasComponent<TransformComponent>() ? Vector3.One : GetComponent<TransformComponent>().Scale;

    protected void SetScale(Vector3 scale)
    {
        if (!HasComponent<TransformComponent>())
            return;

        var transform = GetComponent<TransformComponent>();
        transform.Scale = scale;
        AddComponent(transform);
    }

    protected Vector3 GetForward()
    {
        var rotation = GetRotation();

        // Use rotation to calculate the forward vector
        var cosY = MathF.Cos(rotation.Y);
        var sinY = MathF.Sin(rotation.Y);
        var cosX = MathF.Cos(rotation.X);
        var sinX = MathF.Sin(rotation.X);

        return Vector3.Normalize(new Vector3(
            sinY * cosX,
            -sinX,
            cosY * cosX
        ));
    }

    protected Vector3 GetRight()
    {
        // Right is perpendicular to forward and up
        return Vector3.Normalize(Vector3.Cross(GetForward(), Vector3.UnitY));
    }

    protected Vector3 GetUp()
    {
        // Up is perpendicular to forward and right
        return Vector3.Normalize(Vector3.Cross(GetRight(), GetForward()));
    }

    #endregion

    #region Reflection Utilities for Editor and Serialization

    /// <summary>
    /// Returns all public fields and properties (with public getter/setter) that are editable in the editor.
    /// Uses cached reflection metadata to minimize allocations and improve performance during serialization
    /// and editor updates.
    /// </summary>
    public IEnumerable<(string Name, Type Type, object Value)> GetExposedFields()
    {
        var type = GetType();

        // Get cached fields or compute and cache them
        var fields = _fieldCache.GetOrAdd(type, t =>
            t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => IsSupportedType(f.FieldType))
                .ToArray());

        // Yield field values using cached metadata
        foreach (var field in fields)
        {
            yield return (field.Name, field.FieldType, field.GetValue(this));
        }

        // Get cached properties or compute and cache them
        var properties = _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && IsSupportedType(p.PropertyType))
                .ToArray());

        // Yield property values using cached metadata
        foreach (var prop in properties)
        {
            yield return (prop.Name, prop.PropertyType, prop.GetValue(this));
        }
    }

    /// <summary>
    /// Gets the value of a public field or property by name.
    /// </summary>
    public object GetFieldValue(string name)
    {
        var type = GetType();

        // Search in cached fields
        var fields = _fieldCache.GetOrAdd(type, t =>
            t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => IsSupportedType(f.FieldType))
                .ToArray());

        var field = Array.Find(fields, f => f.Name == name);
        if (field != null)
            return field.GetValue(this);

        // Search in cached properties
        var properties = _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && IsSupportedType(p.PropertyType))
                .ToArray());

        var prop = Array.Find(properties, p => p.Name == name);
        if (prop != null && prop.CanRead)
            return prop.GetValue(this);

        throw new ArgumentException($"Field or property '{name}' not found or not supported.");
    }

    /// <summary>
    /// Sets the value of a public field or property by name.
    /// </summary>
    public void SetFieldValue(string name, object value)
    {
        var type = GetType();

        // Search in cached fields
        var fields = _fieldCache.GetOrAdd(type, t =>
            t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => IsSupportedType(f.FieldType))
                .ToArray());

        var field = Array.Find(fields, f => f.Name == name);
        if (field != null)
        {
            field.SetValue(this, ConvertToSupportedType(value, field.FieldType));
            return;
        }

        // Search in cached properties
        var properties = _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && IsSupportedType(p.PropertyType))
                .ToArray());

        var prop = Array.Find(properties, p => p.Name == name);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(this, ConvertToSupportedType(value, prop.PropertyType));
            return;
        }

        throw new ArgumentException($"Field or property '{name}' not found or not supported.");
    }

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
               type == typeof(bool) || type == typeof(string) ||
               type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4);
    }

    private static object ConvertToSupportedType(object? value, Type targetType)
    {
        if (value == null) 
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        
        if (targetType.IsInstanceOfType(value)) 
            return value;
        
        if (targetType == typeof(Vector2) && value is System.Text.Json.Nodes.JsonArray { Count: 2 } arr2)
            return new Vector2((float)arr2[0]!, (float)arr2[1]!);
        
        if (targetType == typeof(Vector3) && value is System.Text.Json.Nodes.JsonArray { Count: 3 } arr3)
            return new Vector3((float)arr3[0]!, (float)arr3[1]!, (float)arr3[2]!);
        
        if (targetType == typeof(Vector4) && value is System.Text.Json.Nodes.JsonArray { Count: 4 } arr4)
            return new Vector4((float)arr4[0]!, (float)arr4[1]!, (float)arr4[2]!, (float)arr4[3]!);
        
        return Convert.ChangeType(value, targetType);
    }

    #endregion
}