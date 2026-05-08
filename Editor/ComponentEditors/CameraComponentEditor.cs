using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using Math;
using SceneComponents.Camera;

namespace Editor.ComponentEditors;

public class CameraComponentEditor(ISceneContext sceneContext) : IComponentEditor
{
    private static readonly string[] ProjectionTypeStrings = ["Perspective", "Orthographic"];

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<CameraComponent>("Camera", entity, () =>
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();

            UIPropertyRenderer.DrawPropertyField("Primary", cameraComponent.Primary,
                newValue =>
                {
                    if ((bool)newValue)
                        sceneContext.ActiveScene.SetPrimaryCamera(entity);
                    else
                        cameraComponent.Primary = false;
                });

            LayoutDrawer.DrawComboBox("Projection", ProjectionTypeStrings[(int)cameraComponent.ProjectionType],
                ProjectionTypeStrings,
                selectedType =>
                {
                    cameraComponent.ProjectionType = selectedType switch
                    {
                        "Perspective" => CameraProjectionTypeData.Perspective,
                        "Orthographic" => CameraProjectionTypeData.Orthographic,
                        _ => cameraComponent.ProjectionType
                    };
                });

            if (cameraComponent.ProjectionType == CameraProjectionTypeData.Perspective)
            {
                var verticalFov = MathHelpers.RadiansToDegrees(cameraComponent.PerspectiveFOV);
                UIPropertyRenderer.DrawPropertyField("Vertical FOV", verticalFov,
                    newValue => cameraComponent.PerspectiveFOV = MathHelpers.DegreesToRadians((float)newValue));

                UIPropertyRenderer.DrawPropertyField("Near", cameraComponent.PerspectiveNear,
                    newValue => cameraComponent.PerspectiveNear = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Far", cameraComponent.PerspectiveFar,
                    newValue => cameraComponent.PerspectiveFar = (float)newValue);
            }
            else if (cameraComponent.ProjectionType == CameraProjectionTypeData.Orthographic)
            {
                UIPropertyRenderer.DrawPropertyField("Size", cameraComponent.OrthographicSize,
                    newValue => cameraComponent.OrthographicSize = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Near", cameraComponent.OrthographicNear,
                    newValue => cameraComponent.OrthographicNear = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Far", cameraComponent.OrthographicFar,
                    newValue => cameraComponent.OrthographicFar = (float)newValue);

                UIPropertyRenderer.DrawPropertyField("Fixed Aspect Ratio", cameraComponent.FixedAspectRatio,
                    newValue => cameraComponent.FixedAspectRatio = (bool)newValue);
            }
        });
    }
}