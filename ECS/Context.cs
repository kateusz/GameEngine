namespace ECS;

public class Context
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();
    
    public IList<Entity> Entities { get; private set; }

    private Context()
    {
        Entities = new List<Entity>();
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