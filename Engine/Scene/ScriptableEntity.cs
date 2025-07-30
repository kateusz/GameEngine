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

    /// <summary>
    /// Called when the script component is enabled.
    /// </summary>
    public virtual void OnEnable()
    {
    }

    /// <summary>
    /// Called when the script component is disabled.
    /// </summary>
    public virtual void OnDisable()
    {
    }

    #endregion

    #region Input Event Methods

    /// <summary>
    /// Called when a key is pressed.
    /// </summary>
    /// <param name="keyCode">The code of the key that was pressed</param>
    public virtual void OnKeyPressed(KeyCodes key)
    {
        // Key press handling - override in derived classes
    }

    /// <summary>
    /// Called when a key is released.
    /// </summary>
    /// <param name="keyCode">The code of the key that was released</param>
    public virtual void OnKeyReleased(KeyCodes keyCode)
    {
    }

    /// <summary>
    /// Called when a mouse button is pressed.
    /// </summary>
    /// <param name="button">The button that was pressed (0 = left, 1 = right, 2 = middle)</param>
    public virtual void OnMouseButtonPressed(int button)
    {
        // Mouse button press handling - override in derived classes
    }

    /// <summary>
    /// Called when a mouse button is released.
    /// </summary>
    /// <param name="button">The button that was released (0 = left, 1 = right, 2 = middle)</param>
    public virtual void OnMouseButtonReleased(int button)
    {
        // Mouse button release handling - override in derived classes
    }

    /// <summary>
    /// Called when the mouse is moved.
    /// </summary>
    /// <param name="position">The new mouse position</param>
    public virtual void OnMouseMoved(Vector2 position)
    {
        // Mouse movement handling - override in derived classes
    }

    /// <summary>
    /// Called when the mouse wheel is scrolled.
    /// </summary>
    /// <param name="offset">The scroll offset (positive = up, negative = down)</param>
    public virtual void OnMouseScrolled(Vector2 offset)
    {
        // Mouse scroll handling - override in derived classes
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

    /// <summary>
    /// Get a component from the entity this script is attached to.
    /// </summary>
    /// <typeparam name="T">The component type to get</typeparam>
    /// <returns>The component instance, or null if not found</returns>
    public T GetComponent<T>() where T : IComponent
    {
        return Entity.GetComponent<T>();
    }

    /// <summary>
    /// Check if the entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type to check for</typeparam>
    /// <returns>True if the entity has the component, false otherwise</returns>
    public bool HasComponent<T>() where T : IComponent
    {
        return Entity.HasComponent<T>();
    }

    /// <summary>
    /// Add a component to the entity.
    /// </summary>
    /// <typeparam name="T">The component type to add</typeparam>
    /// <returns>The newly added component</returns>
    public T AddComponent<T>() where T : IComponent, new()
    {
        return Entity.AddComponent<T>();
    }

    /// <summary>
    /// Add a component to the entity.
    /// </summary>
    /// <typeparam name="T">The component type to add</typeparam>
    /// <returns>The newly added component</returns>
    public void AddComponent<T>(T component) where T : IComponent
    {
        Entity.AddComponent(component);
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
    protected Entity? FindEntity(string name)
    {
        var currentScene = CurrentScene.Instance;
        if (currentScene == null) return null;

        foreach (var entity in currentScene.Entities)
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
        return CurrentScene.Instance?.CreateEntity(name);
    }

    /// <summary>
    /// Destroy an entity in the current scene.
    /// </summary>
    /// <param name="entity">The entity to destroy</param>
    protected void DestroyEntity(Entity entity)
    {
        CurrentScene.Instance?.DestroyEntity(entity);
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

        return GetComponent<TransformComponent>()!.Translation;
    }

    /// <summary>
    /// Set the position of this entity.
    /// </summary>
    /// <param name="position">The new world position</param>
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

        var transform = GetComponent<TransformComponent>();
        transform.Rotation = rotation;
        AddComponent(transform);
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

        var transform = GetComponent<TransformComponent>();
        transform.Scale = scale;
        AddComponent(transform);
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

    #region Reflection Utilities for Editor and Serialization

    /// <summary>
    /// Returns all public fields and properties (with public getter/setter) that are editable in the editor.
    /// </summary>
    public IEnumerable<(string Name, Type Type, object Value)> GetExposedFields()
    {
        var type = GetType();
        // Public instance fields
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance |
                                             System.Reflection.BindingFlags.Public))
        {
            if (IsSupportedType(field.FieldType))
                yield return (field.Name, field.FieldType, field.GetValue(this));
        }

        // Public instance properties with getter and setter
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.Public))
        {
            if (prop.CanRead && prop.CanWrite && IsSupportedType(prop.PropertyType))
                yield return (prop.Name, prop.PropertyType, prop.GetValue(this));
        }
    }

    /// <summary>
    /// Gets the value of a public field or property by name.
    /// </summary>
    public object GetFieldValue(string name)
    {
        var type = GetType();
        var field = type.GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null && IsSupportedType(field.FieldType))
            return field.GetValue(this);
        var prop = type.GetProperty(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.CanRead && IsSupportedType(prop.PropertyType))
            return prop.GetValue(this);
        throw new ArgumentException($"Field or property '{name}' not found or not supported.");
    }

    /// <summary>
    /// Sets the value of a public field or property by name.
    /// </summary>
    public void SetFieldValue(string name, object value)
    {
        var type = GetType();
        var field = type.GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null && IsSupportedType(field.FieldType))
        {
            field.SetValue(this, ConvertToSupportedType(value, field.FieldType));
            return;
        }

        var prop = type.GetProperty(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null && prop.CanWrite && IsSupportedType(prop.PropertyType))
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

    private static object ConvertToSupportedType(object value, Type targetType)
    {
        if (value == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        if (targetType.IsAssignableFrom(value.GetType())) return value;
        if (targetType == typeof(Vector2) && value is System.Text.Json.Nodes.JsonArray arr2 && arr2.Count == 2)
            return new Vector2((float)arr2[0]!, (float)arr2[1]!);
        if (targetType == typeof(Vector3) && value is System.Text.Json.Nodes.JsonArray arr3 && arr3.Count == 3)
            return new Vector3((float)arr3[0]!, (float)arr3[1]!, (float)arr3[2]!);
        if (targetType == typeof(Vector4) && value is System.Text.Json.Nodes.JsonArray arr4 && arr4.Count == 4)
            return new Vector4((float)arr4[0]!, (float)arr4[1]!, (float)arr4[2]!, (float)arr4[3]!);
        return Convert.ChangeType(value, targetType);
    }

    #endregion
}