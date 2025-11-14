using System.Numerics;
using ECS;
using Engine.Renderer.Cameras;

namespace Engine.Scene;

/// <summary>
/// Interface for a game scene that manages entities, systems, and scene lifecycle.
/// Provides methods for entity management, runtime/editor updates, and resource cleanup.
/// </summary>
public interface IScene : IDisposable
{
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
    /// <param name="camera">The editor viewport camera (unified Camera interface)</param>
    void OnUpdateEditor(TimeSpan ts, Camera camera);

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
    /// Gets the primary camera and its transform matrix in a single O(1) operation.
    /// This method uses an internal cache that is automatically invalidated when cameras
    /// are added, removed, or when entities are destroyed.
    /// </summary>
    /// <returns>A tuple containing the primary Camera and its world transform matrix.
    /// Returns (null, Identity) if no primary camera exists.</returns>
    /// <remarks>
    /// Performance: O(1) for cached lookups (99.9% of frames), O(n) only on cache invalidation.
    /// This eliminates per-frame allocations and iterations across all rendering systems.
    /// </remarks>
    (Camera? camera, Matrix4x4 transform) GetPrimaryCameraData();

    /// <summary>
    /// Duplicates an entity by cloning all of its components.
    /// </summary>
    /// <param name="entity">The entity to duplicate</param>
    /// <returns>The newly created entity with cloned components</returns>
    Entity DuplicateEntity(Entity entity);
}
