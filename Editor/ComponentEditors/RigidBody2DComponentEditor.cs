using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class RigidBody2DComponentEditor : IComponentEditor
{
    private static readonly string[] BodyTypeStrings =
        [nameof(RigidBodyType.Static), nameof(RigidBodyType.Dynamic), nameof(RigidBodyType.Kinematic)];

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<RigidBody2DComponent>("Rigidbody 2D", e, entity =>
        {
            var component = entity.GetComponent<RigidBody2DComponent>();
            
            var currentBodyTypeString = component.BodyType.ToString();
            UIPropertyRenderer.DrawPropertyRow("Body Type", () =>
            {
                if (ImGui.BeginCombo("##BodyType", currentBodyTypeString))
                {
                    for (var i = 0; i < BodyTypeStrings.Length; i++)
                    {
                        var isSelected = currentBodyTypeString == BodyTypeStrings[i];
                        if (ImGui.Selectable(BodyTypeStrings[i], isSelected))
                        {
                            component.BodyType = (RigidBodyType)i;
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
            });

            UIPropertyRenderer.DrawPropertyField("Fixed Rotation", component.FixedRotation,
                newValue => component.FixedRotation = (bool)newValue);
        });
    }
}