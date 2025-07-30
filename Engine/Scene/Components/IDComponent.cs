using ECS;

namespace Engine.Scene.Components;

public record struct IdComponent(long Id) : IComponent
{
    public IdComponent() : this(0) { }
}