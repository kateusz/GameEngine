using System.Numerics;
using ECS;
using SceneComponents;

namespace Editor.Selection;

/// <summary>
/// Per-axis value when editing multiple entities. Null means values differ across selection.
/// </summary>
public readonly struct MixedVector3
{
    public float? X { get; init; }
    public float? Y { get; init; }
    public float? Z { get; init; }

    public Vector3 ToEditBuffer(float mixedDefault = 0f) =>
        new(X ?? mixedDefault, Y ?? mixedDefault, Z ?? mixedDefault);
}

public static class MixedValue
{
    public static MixedVector3 GetMixedVector3(
        IReadOnlyList<Entity> entities,
        Func<TransformComponent, Vector3> selector)
    {
        if (entities.Count == 0)
            return new MixedVector3();

        var first = selector(entities[0].GetComponent<TransformComponent>());
        float? x = first.X, y = first.Y, z = first.Z;

        for (var i = 1; i < entities.Count; i++)
        {
            var v = selector(entities[i].GetComponent<TransformComponent>());
            if (x.HasValue && v.X != x.Value) x = null;
            if (y.HasValue && v.Y != y.Value) y = null;
            if (z.HasValue && v.Z != z.Value) z = null;
        }

        return new MixedVector3 { X = x, Y = y, Z = z };
    }
}
