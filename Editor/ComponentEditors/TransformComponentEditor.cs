using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.Selection;
using Editor.UI.Elements;
using Math;
using SceneComponents;

namespace Editor.ComponentEditors;

public class TransformComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<TransformComponent>("Transform", entity, () =>
        {
            var tc = entity.GetComponent<TransformComponent>();
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

    public static void DrawMulti(IReadOnlyList<Entity> entities)
    {
        ComponentEditorRegistry.DrawComponent<TransformComponent>("Transform", entities, () =>
        {
            DrawMixedVector3(entities, "Translation", tc => tc.Translation, (tc, v) => tc.Translation = v);
            DrawMixedVector3(entities, "Rotation", tc => MathHelpers.ToDegrees(tc.Rotation),
                (tc, v) => tc.Rotation = MathHelpers.ToRadians(v));
            DrawMixedVector3(entities, "Scale", tc => tc.Scale, (tc, v) => tc.Scale = v, resetValue: 1.0f);
        });
    }

    private static void DrawMixedVector3(
        IReadOnlyList<Entity> entities,
        string label,
        Func<TransformComponent, Vector3> selector,
        Action<TransformComponent, Vector3> setter,
        float resetValue = 0f)
    {
        var mixed = MixedValue.GetMixedVector3(entities, selector);
        var editBuffer = mixed.ToEditBuffer();
        var before = editBuffer;

        if (!VectorPanel.DrawVec3Control(label, mixed, ref editBuffer, resetValue))
            return;

        foreach (var entity in entities)
        {
            var tc = entity.GetComponent<TransformComponent>();
            var current = selector(tc);
            var next = current;

            if (mixed.X.HasValue)
            {
                if (editBuffer.X != mixed.X.Value)
                    next.X = editBuffer.X;
            }
            else if (editBuffer.X != before.X)
            {
                next.X = editBuffer.X;
            }

            if (mixed.Y.HasValue)
            {
                if (editBuffer.Y != mixed.Y.Value)
                    next.Y = editBuffer.Y;
            }
            else if (editBuffer.Y != before.Y)
            {
                next.Y = editBuffer.Y;
            }

            if (mixed.Z.HasValue)
            {
                if (editBuffer.Z != mixed.Z.Value)
                    next.Z = editBuffer.Z;
            }
            else if (editBuffer.Z != before.Z)
            {
                next.Z = editBuffer.Z;
            }

            if (next != current)
                setter(tc, next);
        }
    }
}
