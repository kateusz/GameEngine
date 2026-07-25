using ECS;

namespace SceneComponents;

/// <summary>
/// Optional parent link. Absent component or null ParentId means the entity is a scene root.
/// Children lists are derived on the scene — do not store them here.
/// </summary>
public class ParentComponent : IComponent
{
    public int? ParentId { get; set; }

    public ParentComponent()
    {
    }

    public ParentComponent(int? parentId)
    {
        ParentId = parentId;
    }

    public IComponent Clone()
    {
        return new ParentComponent(ParentId);
    }
}
