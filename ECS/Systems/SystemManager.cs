namespace ECS.Systems;

public class SystemManager : IDisposable
{
    private readonly List<ISystem> _systems = [];
    private bool _disposed;

    public IReadOnlyList<ISystem> Systems => _systems;

    public int SystemCount => _systems.Count;

    public bool IsInitialized { get; private set; }

    public void RegisterSystem(ISystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (_systems.Contains(system))
            return;

        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("SystemManager is already initialized.");

        IsInitialized = true;
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
        if (!IsInitialized)
            return;

        for (var i = _systems.Count - 1; i >= 0; i--)
            _systems[i].OnShutdown();

        IsInitialized = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Shutdown();
        foreach (var system in _systems)
        {
            if (system is IDisposable disposable)
                disposable.Dispose();
        }

        _systems.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
