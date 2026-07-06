using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class BoxCollider2DComponentEditor(UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<BoxCollider2DComponent>("Box Collider 2D", entity, () =>
        {
            var component = entity.GetComponent<BoxCollider2DComponent>();

            propertyRenderer.DrawPropertyField("Offset", component.Offset,
                newValue => component.Offset = (System.Numerics.Vector2)newValue);

            propertyRenderer.DrawPropertyField("Size", component.Size,
                newValue => component.Size = (System.Numerics.Vector2)newValue);

            propertyRenderer.DrawPropertyField("Density", component.Density,
                newValue => component.Density = (float)newValue);

            propertyRenderer.DrawPropertyField("Friction", component.Friction,
                newValue => component.Friction = (float)newValue);

            propertyRenderer.DrawPropertyField("Restitution", component.Restitution,
                newValue => component.Restitution = (float)newValue);
        });
    }
}