namespace ECS;

/// <summary>
/// Manages the lifecycle and execution of systems in the Entity Component System.
/// Systems are executed in ascending priority order.
/// Supports both per-scene systems and shared singleton systems with proper lifecycle management.
/// </summary>
public class SystemManager : IDisposable
{
    private readonly List<ISystem> _systems = [];
    private readonly HashSet<ISystem> _sharedSystems = [];
    private bool _disposed = false;

    /// <summary>
    /// Registers a system with the manager, optionally marking it as shared across scenes.
    /// Shared systems have application-wide lifetime and will not be shut down when individual scenes stop.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <param name="isShared">True if the system is shared across all scenes (singleton), false if per-scene.</param>
    /// <exception cref="ArgumentNullException">Thrown when system is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to register a system that is already registered.</exception>
    public void RegisterSystem(ISystem system, bool isShared = false)
    {
        if (system == null)
            throw new ArgumentNullException(nameof(system));

        if (_systems.Contains(system))
            throw new InvalidOperationException("System is already registered.");

        _systems.Add(system);

        if (isShared)
        {
            _sharedSystems.Add(system);
        }

        // Sort systems by priority after adding
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Initializes all registered systems by calling their OnInit method.
    /// Systems are initialized in priority order.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Initialize is called more than once.</exception>
    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("SystemManager is already initialized.");

        IsInitialized = true;

        foreach (var system in _systems)
        {
            system.OnInit();
        }
    }

    /// <summary>
    /// Updates all registered systems by calling their OnUpdate method.
    /// Systems are updated in priority order (ascending).
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <exception cref="InvalidOperationException">Thrown when Update is called before Initialize.</exception>
    public void Update(TimeSpan deltaTime)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("SystemManager must be initialized before updating.");

        foreach (var system in _systems)
        {
            system.OnUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Shuts down per-scene systems by calling their OnShutdown method.
    /// Shared systems are NOT shut down (they have application-wide lifetime).
    /// Systems are shut down in reverse priority order.
    /// </summary>
    /// <remarks>
    /// Only per-scene systems are shut down because shared singleton systems are reused across
    /// multiple scenes. Calling OnShutdown on shared systems here would violate the one-init-one-shutdown
    /// contract when the same singleton is registered in multiple scenes.
    /// Use ShutdownAll() for global cleanup when the application is closing.
    /// </remarks>
    public void Shutdown()
    {
        // Shutdown in reverse order, but ONLY per-scene systems
        for (var i = _systems.Count - 1; i >= 0; i--)
        {
            var system = _systems[i];

            // Skip shared systems - they have application lifetime, not scene lifetime
            if (_sharedSystems.Contains(system))
                continue;

            system.OnShutdown();
        }

        _systems.Clear();
        _sharedSystems.Clear(); // Clear the tracking set
        IsInitialized = false;
    }

    /// <summary>
    /// Shuts down ALL registered systems, including shared singleton systems.
    /// Should only be called during application-wide cleanup, not per-scene cleanup.
    /// Systems are shut down in reverse priority order.
    /// </summary>
    /// <remarks>
    /// This method is intended for global cleanup when the application is closing.
    /// For per-scene cleanup, use Shutdown() instead, which only shuts down per-scene systems.
    /// </remarks>
    public void ShutdownAll()
    {
        // Shutdown ALL systems in reverse order, including shared ones
        for (var i = _systems.Count - 1; i >= 0; i--)
        {
            _systems[i].OnShutdown();
        }

        _systems.Clear();
        _sharedSystems.Clear();
        IsInitialized = false;
    }

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public int SystemCount => _systems.Count;

    /// <summary>
    /// Gets whether the SystemManager has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Disposes the SystemManager and cleans up per-scene systems.
    /// Only disposes systems that are per-scene instances (IDisposable), not shared singleton systems.
    /// </summary>
    /// <remarks>
    /// This method only disposes per-scene systems like PhysicsSimulationSystem.
    /// Singleton systems (SpriteRenderingSystem, ModelRenderingSystem, etc.) are NOT disposed
    /// as they are shared across multiple scenes and managed by the SceneSystemRegistry.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        // Only dispose per-scene systems (systems that implement IDisposable)
        // Singleton systems are shared and should NOT be disposed here
        foreach (var system in _systems)
        {
            if (system is IDisposable disposableSystem)
            {
                disposableSystem.Dispose();
            }
        }

        _systems.Clear();
        _disposed = true;
    }
}
