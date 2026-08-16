using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class BoxCollider3DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<BoxCollider3DComponent>(history)
{
    protected override string DisplayName => "Box Collider 3D";

    protected override void DrawContent(BoxCollider3DComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Offset", component.Offset,
            newValue => component.Offset = (System.Numerics.Vector3)newValue);
        propertyRenderer.DrawPropertyField("Size", component.Size,
            newValue => component.Size = (System.Numerics.Vector3)newValue);
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
