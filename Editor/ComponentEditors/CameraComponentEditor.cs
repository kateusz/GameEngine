using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;
using Math;
using SceneComponents.Camera;

namespace Editor.ComponentEditors;

public class CameraComponentEditor(
    ISceneContext sceneContext,
    UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    private static readonly string[] ProjectionTypeStrings = ["Perspective", "Orthographic"];

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<CameraComponent>("Camera", entity, () =>
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();

            propertyRenderer.DrawPropertyField("Primary", cameraComponent.Primary,
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
                propertyRenderer.DrawPropertyField("Vertical FOV", verticalFov,
                    newValue => cameraComponent.PerspectiveFOV = MathHelpers.DegreesToRadians((float)newValue));

                propertyRenderer.DrawPropertyField("Near", cameraComponent.PerspectiveNear,
                    newValue => cameraComponent.PerspectiveNear = (float)newValue);

                propertyRenderer.DrawPropertyField("Far", cameraComponent.PerspectiveFar,
                    newValue => cameraComponent.PerspectiveFar = (float)newValue);
            }
            else if (cameraComponent.ProjectionType == CameraProjectionTypeData.Orthographic)
            {
                propertyRenderer.DrawPropertyField("Size", cameraComponent.OrthographicSize,
                    newValue => cameraComponent.OrthographicSize = (float)newValue);

                propertyRenderer.DrawPropertyField("Near", cameraComponent.OrthographicNear,
                    newValue => cameraComponent.OrthographicNear = (float)newValue);

                propertyRenderer.DrawPropertyField("Far", cameraComponent.OrthographicFar,
                    newValue => cameraComponent.OrthographicFar = (float)newValue);

                propertyRenderer.DrawPropertyField("Fixed Aspect Ratio", cameraComponent.FixedAspectRatio,
                    newValue => cameraComponent.FixedAspectRatio = (bool)newValue);
            }
        });
    }
}