using ECS;
using Engine.Renderer.Cameras;

namespace Engine.Scene;

/// <summary>
/// Interface for a game scene that manages entities, systems, and scene lifecycle.
/// Provides methods for entity management, runtime/editor updates, and resource cleanup.
/// </summary>
public interface IScene : IDisposable
{
    public string Name { get; }
    
    /// <summary>
    /// Gets all entities in the scene.
    /// </summary>
    IEnumerable<Entity> Entities { get; }

    /// <summary>
    /// Creates a new entity with the specified name.
    /// </summary>
    /// <param name="name">Name for the entity</param>
    /// <returns>The newly created entity</returns>
    Entity CreateEntity(string name);

    /// <summary>
    /// Adds an existing entity to the scene (typically used during deserialization).
    /// </summary>
    /// <param name="entity">The entity to add</param>
    void AddEntity(Entity entity);

    /// <summary>
    /// Destroys an entity, removing it from the scene.
    /// </summary>
    /// <param name="entity">The entity to destroy</param>
    void DestroyEntity(Entity entity);

    /// <summary>
    /// Called when entering runtime/play mode.
    /// Initializes systems and physics bodies.
    /// </summary>
    void OnRuntimeStart();

    /// <summary>
    /// Called when exiting runtime/play mode.
    /// Cleans up physics bodies and calls script OnDestroy methods.
    /// </summary>
    void OnRuntimeStop();

    /// <summary>
    /// Updates the scene in runtime mode (with physics and scripts).
    /// </summary>
    /// <param name="ts">Time elapsed since last update</param>
    void OnUpdateRuntime(TimeSpan ts);

    /// <summary>
    /// Updates the scene in editor mode (without running physics or scripts).
    /// </summary>
    /// <param name="ts">Time elapsed since last update</param>
    /// <param name="camera">The editor viewport camera</param>
    void OnUpdateEditor(TimeSpan ts, EditorCamera camera);
    /// <summary>
    /// Called when the viewport is resized.
    /// Updates camera aspect ratios.
    /// </summary>
    /// <param name="width">New viewport width</param>
    /// <param name="height">New viewport height</param>
    void OnViewportResize(uint width, uint height);

    /// <summary>
    /// Gets the entity containing the primary camera component.
    /// </summary>
    /// <returns>The primary camera entity, or null if none exists</returns>
    Entity? GetPrimaryCameraEntity();

    /// <summary>
    /// Sets the specified entity as the primary camera, clearing the primary flag on all other cameras.
    /// </summary>
    void SetPrimaryCamera(Entity cameraEntity);

    /// <summary>
    /// Duplicates an entity by cloning all of its components.
    /// </summary>
    /// <param name="entity">The entity to duplicate</param>
    /// <returns>The newly created entity with cloned components</returns>
    Entity DuplicateEntity(Entity entity);

    /// <summary>Returns entities that have no parent (or no TransformComponent).</summary>
    IEnumerable<Entity> GetRootEntities();

    /// <summary>Returns the immediate children of an entity.</summary>
    IEnumerable<Entity> GetChildren(Entity entity);

    /// <summary>
    /// Sets <paramref name="parent"/> as the parent of <paramref name="child"/>.
    /// Pass null to unparent.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="child"/> has no TransformComponent, or (when
    /// <paramref name="parent"/> is non-null) when parent has no TransformComponent,
    /// when parent equals child, or when the assignment would create a cycle
    /// (i.e. <paramref name="parent"/> is a descendant of <paramref name="child"/>).
    /// </exception>
    void SetParent(Entity child, Entity? parent);
}
