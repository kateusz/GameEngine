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
    private bool _disposed;
    private bool _perSceneSystemsShutDown;

    public void RegisterSystem(ISystem system, bool isShared = false)
    {
        if (system == null)
            throw new ArgumentNullException(nameof(system));

        if (_systems.Contains(system))
            throw new InvalidOperationException("System is already registered.");

        _systems.Add(system);

        if (isShared)
            _sharedSystems.Add(system);

        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("SystemManager is already initialized.");

        IsInitialized = true;
        _perSceneSystemsShutDown = false;

        foreach (var system in _systems)
            system.OnInit();
    }

    public void Update(TimeSpan deltaTime)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("SystemManager must be initialized before updating.");

        foreach (var system in _systems)
            system.OnUpdate(deltaTime);
    }

    public void Shutdown()
    {
        if (_perSceneSystemsShutDown)
            return;

        ShutdownPerSceneSystems();
        IsInitialized = false;
        _perSceneSystemsShutDown = true;
    }

    public void ShutdownAll()
    {
        for (var i = _systems.Count - 1; i >= 0; i--)
            _systems[i].OnShutdown();

        _systems.Clear();
        _sharedSystems.Clear();
        IsInitialized = false;
    }

    public int SystemCount => _systems.Count;

    public bool IsInitialized { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_perSceneSystemsShutDown)
            ShutdownPerSceneSystems();

        DisposePerSceneSystems();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ShutdownPerSceneSystems()
    {
        for (var i = _systems.Count - 1; i >= 0; i--)
        {
            var system = _systems[i];
            if (_sharedSystems.Contains(system))
                continue;

            system.OnShutdown();
        }
    }

    private void DisposePerSceneSystems()
    {
        foreach (var system in _systems)
        {
            if (_sharedSystems.Contains(system))
                continue;

            if (system is IDisposable disposableSystem)
                disposableSystem.Dispose();
        }

        _systems.Clear();
        _sharedSystems.Clear();
    }
}
