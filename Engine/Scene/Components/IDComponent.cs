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

    public IComponent Clone()
    {
        // Note: When cloning an entity, the new entity will get a new ID
        // This clone creates a copy with the same ID, but Scene.DuplicateEntity
        // creates a new entity with a new ID anyway
        return new IdComponent(Id);
    }
}