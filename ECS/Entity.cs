namespace ECS;

public class Entity
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    private Dictionary<Type, Component> components;

    public Entity(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        components = new Dictionary<Type, Component>();
    }

    public void AddComponent(Component component)
    {
        components[component.GetType()] = component;
    }

    public void RemoveComponent<T>() where T : Component
    {
        components.Remove(typeof(T));
    }

    public T GetComponent<T>() where T : Component
    {
        components.TryGetValue(typeof(T), out Component component);
        return (T)component;
    }

    public bool HasComponent<T>() where T : Component
    {
        return components.ContainsKey(typeof(T));
    }
    
    public bool HasComponents(Type[] componentTypes)
    {
        foreach (var type in componentTypes)
        {
            if (!components.ContainsKey(type))
            {
                return false;
            }
        }
        return true;
    }
}