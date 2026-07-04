namespace ECS;

public interface IComponentAccessor
{
    void SetEntity(Entity entity);
    T GetComponent<T>() where T : IComponent;
    bool HasComponent<T>() where T : IComponent;
    T AddComponent<T>() where T : IComponent, new();
    void AddComponent<T>(T component) where T : IComponent;
    void RemoveComponent<T>() where T : IComponent;
}

public class ComponentAccessor : IComponentAccessor
{
    private Entity _entity = null!;

    public void SetEntity(Entity entity) => _entity = entity;

    public T GetComponent<T>() where T : IComponent
        => _entity.GetComponent<T>();

    public bool HasComponent<T>() where T : IComponent
        => _entity.HasComponent<T>();

    public T AddComponent<T>() where T : IComponent, new()
        => _entity.AddComponent<T>();

    public void AddComponent<T>(T component) where T : IComponent
        => _entity.AddComponent(component);

    public void RemoveComponent<T>() where T : IComponent
        => _entity.RemoveComponent<T>();
}