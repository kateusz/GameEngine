using System.Numerics;
using ECS;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

/// <summary>
/// Base class for all script components in the engine.
/// Script components provide behavior to entities through inheritance.
/// </summary>
public class ScriptableEntity
{
    /// <summary>
    /// The entity this script is attached to
    /// </summary>
    public Entity? Entity { get; set; }

    /// <summary>
    /// A reference to the scene the entity belongs to.
    /// Provides a cached reference to avoid lookups.
    /// </summary>
    protected Scene CurrentScene;

    #region Lifecycle Methods

    /// <summary>
    /// Called once when the script is first initialized.
    /// Use this for one-time setup that doesn't depend on other entities.
    /// </summary>
    public virtual void Init(Scene currentScene)
    {
        CurrentScene = currentScene;
    }
        
    /// <summary>
    /// Called when the entity with this script is created or enabled.
    /// Use this to initialize components and set up references to other entities.
    /// </summary>
    public virtual void OnCreate() { }
        
    /// <summary>
    /// Called every frame during the update loop.
    /// </summary>
    /// <param name="ts">Time since the last frame</param>
    public virtual void OnUpdate(TimeSpan ts) { }
        
    /// <summary>
    /// Called when the entity with this script is destroyed or disabled.
    /// Use this for cleanup tasks (releasing resources, removing event listeners).
    /// </summary>
    public virtual void OnDestroy() { }
        
    /// <summary>
    /// Called when the script component is enabled.
    /// </summary>
    public virtual void OnEnable() { }
        
    /// <summary>
    /// Called when the script component is disabled.
    /// </summary>
    public virtual void OnDisable() { }
        
    #endregion
        
    #region Input Event Methods
        
    /// <summary>
    /// Called when a key is pressed.
    /// </summary>
    /// <param name="keyCode">The code of the key that was pressed</param>
    public virtual void OnKeyPressed(KeyCodes keyCode) { }
        
    /// <summary>
    /// Called when a key is released.
    /// </summary>
    /// <param name="keyCode">The code of the key that was released</param>
    public virtual void OnKeyReleased(KeyCodes keyCode) { }
        
    /// <summary>
    /// Called when a mouse button is pressed.
    /// </summary>
    /// <param name="button">The button that was pressed (0 = left, 1 = right, 2 = middle)</param>
    public virtual void OnMouseButtonPressed(int button) { }
        
    /// <summary>
    /// Called when a mouse button is released.
    /// </summary>
    /// <param name="button">The button that was released (0 = left, 1 = right, 2 = middle)</param>
    public virtual void OnMouseButtonReleased(int button) { }
        
    /// <summary>
    /// Called when the mouse is moved.
    /// </summary>
    /// <param name="position">The new mouse position</param>
    public virtual void OnMouseMoved(Vector2 position) { }
        
    /// <summary>
    /// Called when the mouse wheel is scrolled.
    /// </summary>
    /// <param name="offset">The scroll offset (positive = up, negative = down)</param>
    public virtual void OnMouseScrolled(float offset) { }
        
    #endregion
        
    #region Physics Event Methods
        
    /// <summary>
    /// Called when this entity begins colliding with another entity.
    /// </summary>
    /// <param name="other">The other entity involved in the collision</param>
    public virtual void OnCollisionBegin(Entity other) { }
        
    /// <summary>
    /// Called when this entity ends colliding with another entity.
    /// </summary>
    /// <param name="other">The other entity involved in the collision</param>
    public virtual void OnCollisionEnd(Entity other) { }
        
    /// <summary>
    /// Called when this entity enters a trigger area.
    /// </summary>
    /// <param name="other">The entity with the trigger collider</param>
    public virtual void OnTriggerEnter(Entity other) { }
        
    /// <summary>
    /// Called when this entity exits a trigger area.
    /// </summary>
    /// <param name="other">The entity with the trigger collider</param>
    public virtual void OnTriggerExit(Entity other) { }
        
    #endregion
        
    #region Component Utility Methods
        
    /// <summary>
    /// Get a component from the entity this script is attached to.
    /// </summary>
    /// <typeparam name="T">The component type to get</typeparam>
    /// <returns>The component instance, or null if not found</returns>
    protected T GetComponent<T>() where T : Component
    {
        return Entity.GetComponent<T>();
    }
        
    /// <summary>
    /// Check if the entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type to check for</typeparam>
    /// <returns>True if the entity has the component, false otherwise</returns>
    protected bool HasComponent<T>() where T : Component
    {
        return Entity.HasComponent<T>();
    }
        
    /// <summary>
    /// Add a component to the entity.
    /// </summary>
    /// <typeparam name="T">The component type to add</typeparam>
    /// <returns>The newly added component</returns>
    protected T AddComponent<T>() where T : Component, new()
    {
        var component = new T();
        Entity.AddComponent(component);
        return component;
    }
        
    /// <summary>
    /// Remove a component from the entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove</typeparam>
    protected void RemoveComponent<T>() where T : Component
    {
        Entity.RemoveComponent<T>();
    }
        
    #endregion
        
    #region Entity Utility Methods
        
    /// <summary>
    /// Find an entity by name in the current scene.
    /// </summary>
    /// <param name="name">The name of the entity to find</param>
    /// <returns>The entity if found, null otherwise</returns>
    protected Entity FindEntity(string name)
    {
        if (CurrentScene == null) return null;
            
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name == name)
                return entity;
        }
            
        return null;
    }
        
    /// <summary>
    /// Create a new entity in the current scene.
    /// </summary>
    /// <param name="name">The name for the new entity</param>
    /// <returns>The newly created entity</returns>
    protected Entity CreateEntity(string name)
    {
        if (CurrentScene == null) return null;
            
        return CurrentScene.CreateEntity(name);
    }
        
    /// <summary>
    /// Destroy an entity in the current scene.
    /// </summary>
    /// <param name="entity">The entity to destroy</param>
    protected void DestroyEntity(Entity entity)
    {
        if (CurrentScene == null) return;
            
        CurrentScene.DestroyEntity(entity);
    }
        
    /// <summary>
    /// Destroy the entity this script is attached to.
    /// </summary>
    protected void DestroySelf()
    {
        if (CurrentScene == null) return;
            
        CurrentScene.DestroyEntity(Entity);
    }
        
    #endregion
        
    #region Transform Utility Methods
        
    /// <summary>
    /// Get the position of this entity.
    /// </summary>
    /// <returns>The world position as a Vector3</returns>
    protected Vector3 GetPosition()
    {
        if (!HasComponent<TransformComponent>())
            return Vector3.Zero;
                
        return GetComponent<TransformComponent>().Translation;
    }
        
    /// <summary>
    /// Set the position of this entity.
    /// </summary>
    /// <param name="position">The new world position</param>
    protected void SetPosition(Vector3 position)
    {
        if (!HasComponent<TransformComponent>())
            return;
                
        GetComponent<TransformComponent>().Translation = position;
    }
        
    /// <summary>
    /// Get the rotation of this entity.
    /// </summary>
    /// <returns>The rotation in radians as a Vector3 (XYZ = pitch, yaw, roll)</returns>
    protected Vector3 GetRotation()
    {
        if (!HasComponent<TransformComponent>())
            return Vector3.Zero;
                
        return GetComponent<TransformComponent>().Rotation;
    }
        
    /// <summary>
    /// Set the rotation of this entity.
    /// </summary>
    /// <param name="rotation">The new rotation in radians</param>
    protected void SetRotation(Vector3 rotation)
    {
        if (!HasComponent<TransformComponent>())
            return;
                
        GetComponent<TransformComponent>().Rotation = rotation;
    }
        
    /// <summary>
    /// Get the scale of this entity.
    /// </summary>
    /// <returns>The scale as a Vector3</returns>
    protected Vector3 GetScale()
    {
        if (!HasComponent<TransformComponent>())
            return Vector3.One;
                
        return GetComponent<TransformComponent>().Scale;
    }
        
    /// <summary>
    /// Set the scale of this entity.
    /// </summary>
    /// <param name="scale">The new scale</param>
    protected void SetScale(Vector3 scale)
    {
        if (!HasComponent<TransformComponent>())
            return;
                
        GetComponent<TransformComponent>().Scale = scale;
    }
        
    /// <summary>
    /// Get the forward direction vector of this entity.
    /// </summary>
    /// <returns>The forward direction as a normalized Vector3</returns>
    protected Vector3 GetForward()
    {
        // Calculate forward direction based on rotation
        var rotation = GetRotation();
            
        // Assuming Z is forward in your coordinate system
        // Use rotation to calculate the forward vector
        float cosY = MathF.Cos(rotation.Y);
        float sinY = MathF.Sin(rotation.Y);
        float cosX = MathF.Cos(rotation.X);
        float sinX = MathF.Sin(rotation.X);
            
        return Vector3.Normalize(new Vector3(
            sinY * cosX,
            -sinX,
            cosY * cosX
        ));
    }
        
    /// <summary>
    /// Get the right direction vector of this entity.
    /// </summary>
    /// <returns>The right direction as a normalized Vector3</returns>
    protected Vector3 GetRight()
    {
        // Right is perpendicular to forward and up
        return Vector3.Normalize(Vector3.Cross(GetForward(), Vector3.UnitY));
    }
        
    /// <summary>
    /// Get the up direction vector of this entity.
    /// </summary>
    /// <returns>The up direction as a normalized Vector3</returns>
    protected Vector3 GetUp()
    {
        // Up is perpendicular to forward and right
        return Vector3.Normalize(Vector3.Cross(GetRight(), GetForward()));
    }
        
    /// <summary>
    /// Make this entity look at a target position.
    /// </summary>
    /// <param name="target">The position to look at</param>
    protected void LookAt(Vector3 target)
    {
        if (!HasComponent<TransformComponent>())
            return;
                
        var transform = GetComponent<TransformComponent>();
        var position = transform.Translation;
            
        // Calculate direction vector
        var direction = Vector3.Normalize(target - position);
            
        // Calculate rotation angles
        float yaw = MathF.Atan2(direction.X, direction.Z);
        float pitch = -MathF.Asin(direction.Y);
            
        // Set rotation
        transform.Rotation = new Vector3(pitch, yaw, 0);
    }
        
    #endregion
}