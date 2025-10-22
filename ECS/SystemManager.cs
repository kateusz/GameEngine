namespace ECS;

/// <summary>
/// Manages the lifecycle and execution of systems in the Entity Component System.
/// Systems are executed in ascending priority order.
/// </summary>
public class SystemManager
{
    private readonly List<ISystem> _systems = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Registers a system with the manager.
    /// If the manager is already initialized, the system will be initialized immediately.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when system is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to register a system that is already registered.</exception>
    public void RegisterSystem(ISystem system)
    {
        if (system == null)
            throw new ArgumentNullException(nameof(system));

        if (_systems.Contains(system))
            throw new InvalidOperationException("System is already registered.");

        _systems.Add(system);

        // Sort systems by priority after adding
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // If already initialized, initialize the new system immediately
        if (_isInitialized)
        {
            system.OnInit();
        }
    }

    /// <summary>
    /// Unregisters a system from the manager.
    /// The system's OnShutdown method will be called before removal.
    /// </summary>
    /// <param name="system">The system to unregister.</param>
    /// <exception cref="ArgumentNullException">Thrown when system is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to unregister a system that is not registered.</exception>
    public void UnregisterSystem(ISystem system)
    {
        if (system == null)
            throw new ArgumentNullException(nameof(system));

        if (!_systems.Contains(system))
            throw new InvalidOperationException("System is not registered.");

        system.OnShutdown();
        _systems.Remove(system);
    }

    /// <summary>
    /// Initializes all registered systems by calling their OnInit method.
    /// Systems are initialized in priority order.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Initialize is called more than once.</exception>
    public void Initialize()
    {
        if (_isInitialized)
            throw new InvalidOperationException("SystemManager is already initialized.");

        _isInitialized = true;

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
        if (!_isInitialized)
            throw new InvalidOperationException("SystemManager must be initialized before updating.");

        foreach (var system in _systems)
        {
            system.OnUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Shuts down all registered systems by calling their OnShutdown method.
    /// Systems are shut down in reverse priority order.
    /// </summary>
    public void Shutdown()
    {
        // Shutdown in reverse order
        for (int i = _systems.Count - 1; i >= 0; i--)
        {
            _systems[i].OnShutdown();
        }

        _systems.Clear();
        _isInitialized = false;
    }

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public int SystemCount => _systems.Count;

    /// <summary>
    /// Gets whether the SystemManager has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;
}
