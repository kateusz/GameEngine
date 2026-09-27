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

        ColliderMaterialDrawer.Draw<CircleCollider2DComponent>(propertyRenderer, entity);
    }
}
