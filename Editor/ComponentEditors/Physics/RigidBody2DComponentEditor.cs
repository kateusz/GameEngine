using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class RigidBody2DComponentEditor(UIPropertyRenderer propertyRenderer)
    : ComponentEditor<RigidBody2DComponent>
{
    private static readonly string[] BodyTypeStrings =
        [nameof(RigidBodyType.Static), nameof(RigidBodyType.Dynamic), nameof(RigidBodyType.Kinematic)];

    protected override string DisplayName => "Rigidbody 2D";

    protected override void DrawContent(RigidBody2DComponent component, Entity entity)
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