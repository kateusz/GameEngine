using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class SphereCollider3DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<SphereCollider3DComponent>(history)
{
    protected override string DisplayName => "Sphere Collider 3D";

    protected override void DrawContent(SphereCollider3DComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Offset", component.Offset,
            newValue => component.Offset = (System.Numerics.Vector3)newValue);
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
