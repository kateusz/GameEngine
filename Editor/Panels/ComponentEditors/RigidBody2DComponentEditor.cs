using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

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

            bool fixedRotation = component.FixedRotation;
            UIPropertyRenderer.DrawPropertyRow("Fixed Rotation", () => ImGui.Checkbox("##FixedRotation", ref fixedRotation));
            if (component.FixedRotation != fixedRotation)
                component.FixedRotation = fixedRotation;
        });
    }
}