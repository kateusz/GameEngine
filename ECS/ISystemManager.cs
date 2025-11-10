namespace ECS;

/// <summary>
/// Interface for managing the lifecycle and execution of systems in the Entity Component System.
/// Systems are executed in ascending priority order.
/// Supports both per-scene systems and shared singleton systems with proper lifecycle management.
/// </summary>
public interface ISystemManager : IDisposable
{
    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    int SystemCount { get; }

    /// <summary>
    /// Gets whether the SystemManager has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Registers a system with the manager, optionally marking it as shared across scenes.
    /// Shared systems have application-wide lifetime and will not be shut down when individual scenes stop.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <param name="isShared">True if the system is shared across all scenes (singleton), false if per-scene.</param>
    /// <exception cref="ArgumentNullException">Thrown when system is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to register a system that is already registered.</exception>
    void RegisterSystem(ISystem system, bool isShared = false);

    /// <summary>
    /// Initializes all registered systems by calling their OnInit method.
    /// Systems are initialized in priority order.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Initialize is called more than once.</exception>
    void Initialize();

    /// <summary>
    /// Updates all registered systems by calling their OnUpdate method.
    /// Systems are updated in priority order (ascending).
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <exception cref="InvalidOperationException">Thrown when Update is called before Initialize.</exception>
    void Update(TimeSpan deltaTime);

    /// <summary>
    /// Shuts down per-scene systems by calling their OnShutdown method.
    /// Shared systems are NOT shut down (they have application-wide lifetime).
    /// Systems are shut down in reverse priority order.
    /// </summary>
    /// <remarks>
    /// Only per-scene systems are shut down because shared singleton systems are reused across
    /// multiple scenes. Use ShutdownAll() for global cleanup when the application is closing.
    /// </remarks>
    void Shutdown();

    /// <summary>
    /// Shuts down ALL registered systems, including shared singleton systems.
    /// Should only be called during application-wide cleanup, not per-scene cleanup.
    /// Systems are shut down in reverse priority order.
    /// </summary>
    /// <remarks>
    /// This method is intended for global cleanup when the application is closing.
    /// For per-scene cleanup, use Shutdown() instead, which only shuts down per-scene systems.
    /// </remarks>
    void ShutdownAll();
}
