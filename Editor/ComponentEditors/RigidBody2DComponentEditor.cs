using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class RigidBody2DComponentEditor : IComponentEditor
{
    private static readonly string[] BodyTypeStrings =
        [nameof(RigidBodyType.Static), nameof(RigidBodyType.Dynamic), nameof(RigidBodyType.Kinematic)];

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<RigidBody2DComponent>("Rigidbody 2D", entity, () =>
        {
            var component = entity.GetComponent<RigidBody2DComponent>();

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

            UIPropertyRenderer.DrawPropertyField("Fixed Rotation", component.FixedRotation,
                newValue => component.FixedRotation = (bool)newValue);
        });
    }
}