using ECS;
using Editor.Panels.Elements;
using Engine.Math;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class CameraComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<CameraComponent>("Camera", e, entity =>
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            var camera = cameraComponent.Camera;

            bool primary = cameraComponent.Primary;
            UIPropertyRenderer.DrawPropertyRow("Primary", () => ImGui.Checkbox("##Primary", ref primary));
            if (cameraComponent.Primary != primary)
                cameraComponent.Primary = primary;

            string[] projectionTypeStrings = { "Perspective", "Orthographic" };
            var currentProjectionType = camera.ProjectionType;
            string currentProjectionTypeString = projectionTypeStrings[(int)currentProjectionType];
            
            UIPropertyRenderer.DrawPropertyRow("Projection", () =>
            {
                if (ImGui.BeginCombo("##Projection", currentProjectionTypeString))
                {
                    for (int i = 0; i < projectionTypeStrings.Length; i++)
                    {
                        bool isSelected = currentProjectionTypeString == projectionTypeStrings[i];
                        if (ImGui.Selectable(projectionTypeStrings[i], isSelected))
                        {
                            camera.SetProjectionType((ProjectionType)i);
                        }
                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
            });

            if (camera.ProjectionType == ProjectionType.Perspective)
            {
                float verticalFov = MathHelpers.RadiansToDegrees(camera.PerspectiveFOV);
                UIPropertyRenderer.DrawPropertyRow("Vertical FOV", () => ImGui.DragFloat("##VerticalFOV", ref verticalFov));
                if (verticalFov != MathHelpers.RadiansToDegrees(camera.PerspectiveFOV))
                    camera.SetPerspectiveVerticalFOV(MathHelpers.DegreesToRadians(verticalFov));

                float perspectiveNear = camera.PerspectiveNear;
                UIPropertyRenderer.DrawPropertyRow("Near", () => ImGui.DragFloat("##PerspectiveNear", ref perspectiveNear));
                if (camera.PerspectiveNear != perspectiveNear)
                    camera.SetPerspectiveNearClip(perspectiveNear);

                float perspectiveFar = camera.PerspectiveFar;
                UIPropertyRenderer.DrawPropertyRow("Far", () => ImGui.DragFloat("##PerspectiveFar", ref perspectiveFar));
                if (camera.PerspectiveFar != perspectiveFar)
                    camera.SetPerspectiveFarClip(perspectiveFar);
            }
            else if (camera.ProjectionType == ProjectionType.Orthographic)
            {
                float orthoSize = camera.OrthographicSize;
                UIPropertyRenderer.DrawPropertyRow("Size", () => ImGui.DragFloat("##OrthoSize", ref orthoSize));
                if (camera.OrthographicSize != orthoSize)
                    camera.SetOrthographicSize(orthoSize);

                float orthoNear = camera.OrthographicNear;
                UIPropertyRenderer.DrawPropertyRow("Near", () => ImGui.DragFloat("##OrthoNear", ref orthoNear));
                if (camera.OrthographicNear != orthoNear)
                    camera.SetOrthographicNearClip(orthoNear);

                float orthoFar = camera.OrthographicFar;
                UIPropertyRenderer.DrawPropertyRow("Far", () => ImGui.DragFloat("##OrthoFar", ref orthoFar));
                if (camera.OrthographicFar != orthoFar)
                    camera.SetOrthographicFarClip(orthoFar);

                bool fixedAspectRatio = cameraComponent.FixedAspectRatio;
                UIPropertyRenderer.DrawPropertyRow("Fixed Aspect Ratio", () => ImGui.Checkbox("##FixedAspectRatio", ref fixedAspectRatio));
                if (cameraComponent.FixedAspectRatio != fixedAspectRatio)
                    cameraComponent.FixedAspectRatio = fixedAspectRatio;
            }
        });
    }
}