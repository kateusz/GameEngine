using System.Numerics;
using ECS;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

/// <summary>
/// Base class for all script components in the engine.
/// </summary>
public abstract class ScriptableEntity
{
    private const string EntityNotSetMessage = "Entity is not set!";
    private ISceneContext? _sceneContext;

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

    public virtual void OnMouseMoved(float x, float y)
    {
    }

    public virtual void OnMouseButtonReleased(int button)
    {
    }

    public virtual void OnMouseScrolled(float xOffset, float yOffset)
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
            throw new InvalidOperationException(EntityNotSetMessage);
        return Entity.GetComponent<T>();
    }

    protected bool HasComponent<T>() where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException(EntityNotSetMessage);
        return Entity.HasComponent<T>();
    }

    protected T AddComponent<T>() where T : IComponent, new()
    {
        if (Entity == null)
            throw new InvalidOperationException(EntityNotSetMessage);
        return Entity.AddComponent<T>();
    }

    protected void AddComponent<T>(T component) where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException(EntityNotSetMessage);
        Entity.AddComponent<T>(component);
    }

    protected void RemoveComponent<T>() where T : IComponent
    {
        if (Entity == null)
            throw new InvalidOperationException(EntityNotSetMessage);
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
        return ExposedMemberAccessor.GetExposedMembers(this);
    }

    /// <summary>
    /// Gets the value of a public field or property by name.
    /// </summary>
    public object GetFieldValue(string name)
    {
        return ExposedMemberAccessor.GetMemberValue(this, name);
    }

    /// <summary>
    /// Sets the value of a public field or property by name.
    /// </summary>
    public void SetFieldValue(string name, object value)
    {
        ExposedMemberAccessor.SetMemberValue(this, name, value);
    }

    #endregion
}