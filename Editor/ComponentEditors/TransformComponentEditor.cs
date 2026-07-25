using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using Math;
using SceneComponents;

namespace Editor.ComponentEditors;

public class TransformComponentEditor(IEditorHistory history) : ComponentEditor<TransformComponent>(history)
{
    protected override string DisplayName => "Transform";

    protected override void DrawContent(TransformComponent component, Entity entity)
    {
        var newTranslation = component.Translation;
        VectorPanel.DrawVec3Control("Translation", ref newTranslation);

        if (newTranslation != component.Translation)
            component.Translation = newTranslation;

        var rotationRadians = component.Rotation;
        var rotationDegrees = MathHelpers.ToDegrees(rotationRadians);
        VectorPanel.DrawVec3Control("Rotation", ref rotationDegrees);
        var newRotationRadians = MathHelpers.ToRadians(rotationDegrees);

        if (newRotationRadians != component.Rotation)
            component.Rotation = newRotationRadians;

        var newScale = component.Scale;
        VectorPanel.DrawVec3Control("Scale", ref newScale, 1.0f);

        if (newScale != component.Scale)
            component.Scale = newScale;
    }
}