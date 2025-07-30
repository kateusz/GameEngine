using ECS;

namespace Engine.Scene.Components;

public class IdComponent : IComponent
{
    public long Id { get; set; }

    public IdComponent()
    {
        Id = 0;
    }

    public IdComponent(long id)
    {
        Id = id;
    }
}