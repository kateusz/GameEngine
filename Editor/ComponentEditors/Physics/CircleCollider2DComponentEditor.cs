using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class CircleCollider2DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<CircleCollider2DComponent>(history)
{
    protected override string DisplayName => "Circle Collider 2D";

    protected override void DrawContent(CircleCollider2DComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Offset", component.Offset,
            newValue => component.Offset = (System.Numerics.Vector2)newValue);

        propertyRenderer.DrawPropertyField("Radius", component.Radius,
            newValue => component.Radius = (float)newValue);

        propertyRenderer.DrawPropertyField("Density", component.Density,
            newValue => component.Density = (float)newValue);

        propertyRenderer.DrawPropertyField("Friction", component.Friction,
            newValue => component.Friction = (float)newValue);

        propertyRenderer.DrawPropertyField("Restitution", component.Restitution,
            newValue => component.Restitution = (float)newValue);

        propertyRenderer.DrawPropertyField("Is Trigger", component.IsTrigger,
            newValue => component.IsTrigger = (bool)newValue);
    }
}
