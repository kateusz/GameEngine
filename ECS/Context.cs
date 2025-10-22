using System.Collections.Concurrent;

namespace ECS;

public class Context
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();
    
    public ConcurrentBag<Entity> Entities { get; set; }

    private Context()
    {
        Entities = new ConcurrentBag<Entity>();
    }

    public void Register(Entity entity)
    {
        Entities.Add(entity);
    }

    public List<Entity> GetGroup(params Type[] types)
    {
        var result = new List<Entity>();
        foreach (var entity in Entities)
        {
            if (entity.HasComponents(types))
            {
                result.Add(entity);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns an enumerable view of all entities that have the specified component type.
    /// Uses value tuples and iterator pattern to dramatically reduce heap allocations.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to query for.</typeparam>
    /// <returns>An enumerable of (Entity, TComponent) tuples representing each entity and its component.</returns>
    /// <remarks>
    /// Note: If you need to materialize the results into a collection, call .ToList() or .ToArray(),
    /// but be aware this will allocate. For best performance, consume results directly via foreach.
    /// </remarks>
    public IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : Component
    {
        foreach (var entity in Entities)
        {
            if (entity.HasComponent<TComponent>())
            {
                var component = entity.GetComponent<TComponent>();
                yield return (entity, component);
            }
        }
    }
}