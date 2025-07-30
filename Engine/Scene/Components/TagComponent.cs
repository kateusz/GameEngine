using ECS;

namespace Engine.Scene.Components;

public record struct TagComponent(string Tag) : IComponent
{
    public TagComponent() : this(string.Empty) { }
}