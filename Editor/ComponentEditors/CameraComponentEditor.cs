using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using Engine.Scene;
using Math;
using SceneComponents.Camera;

namespace Editor.ComponentEditors;

public class CameraComponentEditor(
    ISceneContext sceneContext,
    UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<CameraComponent>(history)
{
    protected override string DisplayName => "Camera";

    protected override void DrawContent(CameraComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("SSAO", entity,
            e => e.GetComponent<CameraComponent>().Ssao,
            (e, v) => e.GetComponent<CameraComponent>().Ssao = v);

        propertyRenderer.DrawPropertyField("SSAO Radius", entity,
            e => e.GetComponent<CameraComponent>().SsaoRadius,
            (e, v) => e.GetComponent<CameraComponent>().SsaoRadius = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("SSAO Strength", entity,
            e => e.GetComponent<CameraComponent>().SsaoStrength,
            (e, v) => e.GetComponent<CameraComponent>().SsaoStrength = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Primary", entity,
            e => e.GetComponent<CameraComponent>().Primary,
            (e, v) =>
            {
                if (v)
                    sceneContext.ActiveScene!.SetPrimaryCamera(e);
                else
                    e.GetComponent<CameraComponent>().Primary = false;
            });

        MultiField.DrawEnumCombo("Projection", entity,
            e => e.GetComponent<CameraComponent>().ProjectionType,
            (e, t) => e.GetComponent<CameraComponent>().ProjectionType = t);

        var allPerspective = MultiField.For(entity).All(e =>
            e.GetComponent<CameraComponent>().ProjectionType == CameraProjectionTypeData.Perspective);
        var allOrthographic = MultiField.For(entity).All(e =>
            e.GetComponent<CameraComponent>().ProjectionType == CameraProjectionTypeData.Orthographic);

        if (allPerspective)
        {
            propertyRenderer.DrawPropertyField("Vertical FOV", entity,
                e => MathHelpers.RadiansToDegrees(e.GetComponent<CameraComponent>().PerspectiveFOV),
                (e, v) => e.GetComponent<CameraComponent>().PerspectiveFOV = MathHelpers.DegreesToRadians(v),
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Near", entity,
                e => e.GetComponent<CameraComponent>().PerspectiveNear,
                (e, v) => e.GetComponent<CameraComponent>().PerspectiveNear = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Far", entity,
                e => e.GetComponent<CameraComponent>().PerspectiveFar,
                (e, v) => e.GetComponent<CameraComponent>().PerspectiveFar = v,
                MultiField.SameFloat);
        }
        else if (allOrthographic)
        {
            propertyRenderer.DrawPropertyField("Size", entity,
                e => e.GetComponent<CameraComponent>().OrthographicSize,
                (e, v) => e.GetComponent<CameraComponent>().OrthographicSize = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Near", entity,
                e => e.GetComponent<CameraComponent>().OrthographicNear,
                (e, v) => e.GetComponent<CameraComponent>().OrthographicNear = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Far", entity,
                e => e.GetComponent<CameraComponent>().OrthographicFar,
                (e, v) => e.GetComponent<CameraComponent>().OrthographicFar = v,
                MultiField.SameFloat);

            propertyRenderer.DrawPropertyField("Fixed Aspect Ratio", entity,
                e => e.GetComponent<CameraComponent>().FixedAspectRatio,
                (e, v) => e.GetComponent<CameraComponent>().FixedAspectRatio = v);
        }
    }
}
