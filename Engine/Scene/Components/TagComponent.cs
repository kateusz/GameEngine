using ECS;

namespace Engine.Scene.Components;

public class TagComponent : IComponent
{
    public string Tag { get; set; }

    public TagComponent()
    {
        Tag = string.Empty;
    }

    public TagComponent(string tag)
    {
        Tag = tag;
    }

    public IComponent Clone()
    {
        return new TagComponent(Tag);
    }
}