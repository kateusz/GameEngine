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
        propertyRenderer.DrawPropertyField("Offset", entity,
            e => e.GetComponent<CircleCollider2DComponent>().Offset,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().Offset = v,
            UIPropertyRenderer.SameVector2);

        propertyRenderer.DrawPropertyField("Radius", entity,
            e => e.GetComponent<CircleCollider2DComponent>().Radius,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().Radius = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Density", entity,
            e => e.GetComponent<CircleCollider2DComponent>().Density,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().Density = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Friction", entity,
            e => e.GetComponent<CircleCollider2DComponent>().Friction,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().Friction = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Restitution", entity,
            e => e.GetComponent<CircleCollider2DComponent>().Restitution,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().Restitution = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Is Trigger", entity,
            e => e.GetComponent<CircleCollider2DComponent>().IsTrigger,
            (e, v) => e.GetComponent<CircleCollider2DComponent>().IsTrigger = v);
    }
}
