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
        ColliderMaterialDrawer.Draw<EdgeCollider2DComponent>(propertyRenderer, entity);
    }
}
