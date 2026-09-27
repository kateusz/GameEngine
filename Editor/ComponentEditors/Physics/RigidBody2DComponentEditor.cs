using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using SceneComponents.Physics;

namespace Editor.ComponentEditors.Physics;

public class RigidBody2DComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<RigidBody2DComponent>(history)
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

        propertyRenderer.DrawPropertyField("Fixed Rotation", entity,
            e => e.GetComponent<RigidBody2DComponent>().FixedRotation,
            (e, v) => e.GetComponent<RigidBody2DComponent>().FixedRotation = v);
        propertyRenderer.DrawPropertyField("Gravity Scale", entity,
            e => e.GetComponent<RigidBody2DComponent>().GravityScale,
            (e, v) => e.GetComponent<RigidBody2DComponent>().GravityScale = v,
            MultiField.SameFloat);
        propertyRenderer.DrawPropertyField("Bullet (CCD)", entity,
            e => e.GetComponent<RigidBody2DComponent>().IsBullet,
            (e, v) => e.GetComponent<RigidBody2DComponent>().IsBullet = v);
    }
}
