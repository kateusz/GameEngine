namespace ECS.Systems;

/// <summary>
/// Manages the lifecycle and execution of systems in the Entity Component System.
/// Systems are executed in ascending priority order.
/// Supports both per-scene systems and shared singleton systems with proper lifecycle management.
/// </summary>
public class SystemManager : ISystemManager
{
    private readonly List<ISystem> _systems = [];
    private readonly HashSet<ISystem> _sharedSystems = [];
    private bool _disposed = false;

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

    public void Update(TimeSpan deltaTime)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("SystemManager must be initialized before updating.");

        foreach (var system in _systems)
        {
            system.OnUpdate(deltaTime);
        }
    }

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

    public int SystemCount => _systems.Count;

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
