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
    UIPropertyRenderer propertyRenderer)
    : ComponentEditor<CameraComponent>
{
    private static readonly string[] ProjectionTypeStrings = ["Perspective", "Orthographic"];

    protected override string DisplayName => "Camera";

    protected override void DrawContent(CameraComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Primary", component.Primary,
            newValue =>
            {
                if ((bool)newValue)
                    sceneContext.ActiveScene.SetPrimaryCamera(entity);
                else
                    component.Primary = false;
            });

        LayoutDrawer.DrawComboBox("Projection", ProjectionTypeStrings[(int)component.ProjectionType],
            ProjectionTypeStrings,
            selectedType =>
            {
                component.ProjectionType = selectedType switch
                {
                    "Perspective" => CameraProjectionTypeData.Perspective,
                    "Orthographic" => CameraProjectionTypeData.Orthographic,
                    _ => component.ProjectionType
                };
            });

        if (component.ProjectionType == CameraProjectionTypeData.Perspective)
        {
            var verticalFov = MathHelpers.RadiansToDegrees(component.PerspectiveFOV);
            propertyRenderer.DrawPropertyField("Vertical FOV", verticalFov,
                newValue => component.PerspectiveFOV = MathHelpers.DegreesToRadians((float)newValue));

            propertyRenderer.DrawPropertyField("Near", component.PerspectiveNear,
                newValue => component.PerspectiveNear = (float)newValue);

            propertyRenderer.DrawPropertyField("Far", component.PerspectiveFar,
                newValue => component.PerspectiveFar = (float)newValue);
        }
        else if (component.ProjectionType == CameraProjectionTypeData.Orthographic)
        {
            propertyRenderer.DrawPropertyField("Size", component.OrthographicSize,
                newValue => component.OrthographicSize = (float)newValue);

            propertyRenderer.DrawPropertyField("Near", component.OrthographicNear,
                newValue => component.OrthographicNear = (float)newValue);

            propertyRenderer.DrawPropertyField("Far", component.OrthographicFar,
                newValue => component.OrthographicFar = (float)newValue);

            propertyRenderer.DrawPropertyField("Fixed Aspect Ratio", component.FixedAspectRatio,
                newValue => component.FixedAspectRatio = (bool)newValue);
        }
    }
}