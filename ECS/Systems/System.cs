namespace ECS.Systems;

public abstract class System
{
    public abstract void Update(List<Entity> entities);
}