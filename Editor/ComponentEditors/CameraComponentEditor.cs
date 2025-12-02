using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Math;
using Engine.Scene;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class CameraComponentEditor : IComponentEditor
{
    private static readonly string[] ProjectionTypeStrings = ["Perspective", "Orthographic"];

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<CameraComponent>("Camera", entity, () =>
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            var camera = cameraComponent.Camera;

            UIPropertyRenderer.DrawPropertyField("Primary", cameraComponent.Primary,
                newValue => cameraComponent.Primary = (bool)newValue);

            LayoutDrawer.DrawComboBox("Projection", ProjectionTypeStrings[(int)camera.ProjectionType],
                ProjectionTypeStrings,
                selectedType =>
                {
                    camera.ProjectionType = selectedType switch
                    {
                        "Perspective" => ProjectionType.Perspective,
                        "Orthographic" => ProjectionType.Orthographic,
                        _ => camera.ProjectionType
                    };
                });

            if (camera.ProjectionType == ProjectionType.Perspective)
            {
                var verticalFov = MathHelpers.RadiansToDegrees(camera.PerspectiveFOV);
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