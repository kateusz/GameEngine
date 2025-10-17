namespace ECS;

public class Context
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();

    private readonly List<Entity> _entities = new();
    private readonly Queue<Action> _deferredCommands = new();
    private readonly object _commandLock = new();

    public IReadOnlyList<Entity> Entities => _entities;

    private Context()
    {
    }

    public void Register(Entity entity)
    {
        lock (_commandLock)
        {
            _deferredCommands.Enqueue(() => _entities.Add(entity));
        }
    }

    public void Remove(Entity entity)
    {
        lock (_commandLock)
        {
            _deferredCommands.Enqueue(() => _entities.Remove(entity));
        }
    }

    public void ProcessDeferredCommands()
    {
        lock (_commandLock)
        {
            while (_deferredCommands.Count > 0)
            {
                var command = _deferredCommands.Dequeue();
                command();
            }
        }
    }

    public List<Entity> GetGroup(params Type[] types)
    {
        var result = new List<Entity>();
        foreach (var entity in _entities)
        {
            if (entity.HasComponents(types))
            {
                result.Add(entity);
            }
        }
        return result;
    }

    public List<Tuple<Entity, TComponent>> View<TComponent>() where TComponent : Component
    {
        var result = new List<Tuple<Entity, TComponent>>();
        var groups = GetGroup(typeof(TComponent));

        foreach (var entity in groups)
        {
            var component = entity.GetComponent<TComponent>();
            result.Add(new Tuple<Entity, TComponent>(entity, component));
        }

        return result;
    }
}