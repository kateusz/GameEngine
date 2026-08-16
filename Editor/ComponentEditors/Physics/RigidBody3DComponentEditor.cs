using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class RigidBody3DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<RigidBody3DComponent>(history)
{
    private static readonly string[] BodyTypeStrings =
        [nameof(RigidBodyType.Static), nameof(RigidBodyType.Dynamic), nameof(RigidBodyType.Kinematic)];

    protected override string DisplayName => "Rigidbody 3D";

    protected override void DrawContent(RigidBody3DComponent component, Entity entity)
    {
        LayoutDrawer.DrawComboBox("Body Type", component.BodyType.ToString(), BodyTypeStrings,
            selectedType =>
            {
                component.BodyType = selectedType switch
                {
                    nameof(RigidBodyType.Static) => RigidBodyType.Static,
                    nameof(RigidBodyType.Dynamic) => RigidBodyType.Dynamic,
                    nameof(RigidBodyType.Kinematic) => RigidBodyType.Kinematic,
                    _ => component.BodyType
                };
            });

        propertyRenderer.DrawPropertyField("Fixed Rotation", component.FixedRotation,
            newValue => component.FixedRotation = (bool)newValue);
        propertyRenderer.DrawPropertyField("Gravity Scale", component.GravityScale,
            newValue => component.GravityScale = (float)newValue);
    }
}
