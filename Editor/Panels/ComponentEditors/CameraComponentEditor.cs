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

            UIPropertyRenderer.DrawPropertyField("Primary", cameraComponent.Primary,
                newValue => cameraComponent.Primary = (bool)newValue);

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
                            camera.ProjectionType = (ProjectionType)i;
                        }
                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
            });

            if (camera.ProjectionType == ProjectionType.Perspective)
            {
                float verticalFov = MathHelpers.RadiansToDegrees(camera.PerspectiveFOV);
                UIPropertyRenderer.DrawPropertyField("Vertical FOV", verticalFov,
                    newValue => camera.PerspectiveFOV = MathHelpers.DegreesToRadians((float)newValue));

                UIPropertyRenderer.DrawPropertyField("Near", camera.PerspectiveNear,
                    newValue => camera.PerspectiveNear = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Far", camera.PerspectiveFar,
                    newValue => camera.PerspectiveFar = (float)newValue);
            }
            else if (camera.ProjectionType == ProjectionType.Orthographic)
            {
                UIPropertyRenderer.DrawPropertyField("Size", camera.OrthographicSize,
                    newValue => camera.OrthographicSize = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Near", camera.OrthographicNear,
                    newValue => camera.OrthographicNear = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Far", camera.OrthographicFar,
                    newValue => camera.OrthographicFar = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Fixed Aspect Ratio", cameraComponent.FixedAspectRatio,
                    newValue => cameraComponent.FixedAspectRatio = (bool)newValue);
            }
        });
    }
}