using System.Numerics;
using Engine.Scene;
using SceneComponents;

namespace Editor.Features.History.Commands;

public sealed class SetTransformCommand(
    IScene scene,
    int entityId,
    Vector3 beforeTranslation,
    Vector3 beforeRotation,
    Vector3 beforeScale,
    Vector3 afterTranslation,
    Vector3 afterRotation,
    Vector3 afterScale) : IUndoCommand
{
    public bool Execute() => Apply(afterTranslation, afterRotation, afterScale);

    public void Undo() => Apply(beforeTranslation, beforeRotation, beforeScale);

    public static bool TrsEqual(
        Vector3 aT, Vector3 aR, Vector3 aS,
        Vector3 bT, Vector3 bR, Vector3 bS)
        => aT == bT && aR == bR && aS == bS;

    private bool Apply(Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        if (!scene.Context.Contains(entityId))
            return false;

        var entity = scene.Context.GetById(entityId);
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return false;

        transform.Translation = translation;
        transform.Rotation = rotation;
        transform.Scale = scale;
        return true;
    }
}
