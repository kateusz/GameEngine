using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class EdgeCollider2DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<EdgeCollider2DComponent>(history)
{
    protected override string DisplayName => "Edge Collider 2D";

    protected override void DrawContent(EdgeCollider2DComponent component, Entity entity)
    {
        ColliderPointListDrawer.DrawVec2List("Points", component.Points, minCount: 2);

        propertyRenderer.DrawPropertyField("Density", entity,
            e => e.GetComponent<EdgeCollider2DComponent>().Density,
            (e, v) => e.GetComponent<EdgeCollider2DComponent>().Density = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Friction", entity,
            e => e.GetComponent<EdgeCollider2DComponent>().Friction,
            (e, v) => e.GetComponent<EdgeCollider2DComponent>().Friction = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Restitution", entity,
            e => e.GetComponent<EdgeCollider2DComponent>().Restitution,
            (e, v) => e.GetComponent<EdgeCollider2DComponent>().Restitution = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Is Trigger", entity,
            e => e.GetComponent<EdgeCollider2DComponent>().IsTrigger,
            (e, v) => e.GetComponent<EdgeCollider2DComponent>().IsTrigger = v);
    }
}
