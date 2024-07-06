namespace ECS;

public class Context
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();
    
    public List<Entity> Entities { get; private set; }

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
}