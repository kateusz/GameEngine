using System.Numerics;
using ECS;
using Engine.Math;
using Engine.Scene.Components;

namespace Editor.Panels.ComponentEditors;

public class TransformComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<TransformComponent>("Transform", e, entity =>
        {
            var tc = entity.GetComponent<TransformComponent>();
            var newTranslation = tc.Translation;
            VectorPanel.DrawVec3Control("Translation", ref newTranslation);

            if (newTranslation != tc.Translation)
                tc.Translation = newTranslation;

            var rotationRadians = tc.Rotation;
            Vector3 rotationDegrees = MathHelpers.ToDegrees(rotationRadians);
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