using Audio;
using ECS;
using Input;
using Scripting;

namespace Scripting;

/// <summary>
/// Base class for all script components in the engine.
/// </summary>
public abstract class ScriptableEntity
{
    private readonly IComponentAccessor _componentAccessor;

    /// <summary>
    /// The entity this script is attached to
    /// </summary>
    private IEntity? _entity;

    protected ScriptableEntity(IComponentAccessor componentAccessor, IAudio audio)
    {
        _componentAccessor = componentAccessor;
        Audio = audio;
    }

    protected IAudio Audio { get; }

    public void SetEntity(Entity entity)
    {
        _entity = entity;
        _componentAccessor.SetEntity(entity);
    }

    public bool IsInitialized => _entity is not null;

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
        return _componentAccessor.GetComponent<T>();
    }

    protected bool HasComponent<T>() where T : IComponent
    {
        return _componentAccessor.HasComponent<T>();
    }

    protected T AddComponent<T>() where T : IComponent, new()
    {
        return _componentAccessor.AddComponent<T>();
    }

    protected void AddComponent<T>(T component) where T : IComponent
    {
        _componentAccessor.AddComponent<T>(component);
    }

    protected void RemoveComponent<T>() where T : IComponent
    {
        _componentAccessor.RemoveComponent<T>();
    }

    #endregion
}