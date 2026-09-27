using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class BoxCollider2DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<BoxCollider2DComponent>(history)
{
    protected override string DisplayName => "Box Collider 2D";

    protected override void DrawContent(BoxCollider2DComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Offset", entity,
            e => e.GetComponent<BoxCollider2DComponent>().Offset,
            (e, v) => e.GetComponent<BoxCollider2DComponent>().Offset = v,
            UIPropertyRenderer.SameVector2);

        propertyRenderer.DrawPropertyField("Size", entity,
            e => e.GetComponent<BoxCollider2DComponent>().Size,
            (e, v) => e.GetComponent<BoxCollider2DComponent>().Size = v,
            UIPropertyRenderer.SameVector2);

        ColliderMaterialDrawer.Draw<BoxCollider2DComponent>(propertyRenderer, entity);
    }
}
