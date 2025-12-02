using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Math;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class TransformComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<TransformComponent>("Transform", e, () =>
        {
            var tc = e.GetComponent<TransformComponent>();
            var newTranslation = tc.Translation;
            VectorPanel.DrawVec3Control("Translation", ref newTranslation);

            if (newTranslation != tc.Translation)
                tc.Translation = newTranslation;

            var rotationRadians = tc.Rotation;
            var rotationDegrees = MathHelpers.ToDegrees(rotationRadians);
            VectorPanel.DrawVec3Control("Rotation", ref rotationDegrees);
            var newRotationRadians = MathHelpers.ToRadians(rotationDegrees);

            if (newRotationRadians != tc.Rotation)
                tc.Rotation = newRotationRadians;

            var newScale = tc.Scale;
            VectorPanel.DrawVec3Control("Scale", ref newScale, 1.0f);

            if (newScale != tc.Scale)
                tc.Scale = newScale;
        });
    }
}