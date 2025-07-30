using ECS;

namespace Engine.Scene.Components;

public struct TagComponent : IComponent
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
}