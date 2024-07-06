using ECS;
using Engine.Renderer;
using Engine.Scene.Components;

namespace Engine.Scene;

public class Scene
{
    private uint _viewportWidth;
    private uint _viewportHeight;

    public Entity CreateEntity(string name)
    {
        var entity = new Entity(name);
        entity.AddComponent(new TransformComponent());
        return entity;
    }

    public void OnUpdate(TimeSpan ts)
    {
        var group = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in group)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();

            Renderer2D.Instance.DrawQuad(transformComponent.Transform, spriteRendererComponent.Color);
        }
    }
}