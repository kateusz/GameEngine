using ECS;

namespace Engine.Scene;

public class ScriptableEntity
{
    public Entity Entity { get; set; }

    public virtual void Init()
    {
    }
    
    public virtual void Destroy()
    {
    }
    
    public virtual void OnCreate()
    {
    }
    
    public virtual void OnDestroy()
    {
    }

    public virtual void OnUpdate(TimeSpan ts)
    {
    }
    
    protected T GetComponent<T>() where T : Component
    {
        return Entity.GetComponent<T>();
    }
}