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