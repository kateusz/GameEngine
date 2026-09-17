using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Window;
using Engine.Renderer.Pipeline;
using Engine.Scene.Cameras;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace Engine.Scene.Systems;

/// <summary>
/// Primary-camera query for render and picking. The instance implements per-scene
/// screen→world (<see cref="ICameraQueries"/>); <see cref="TryGetPrimaryView"/> is the shared lookup.
/// </summary>
internal sealed class CameraQueries(Context context, IPointerSurface pointerSurface) : ICameraQueries
{
    // ponytail: main-thread only; pass a scratch SceneCamera if render goes wide.
    private static readonly SceneCamera Scratch = new();

    public Vector2? ScreenToWorld2D(Vector2 windowPosition)
    {
        if (!pointerSurface.Contains(windowPosition))
            return null;

        if (!TryGetPrimaryView(context, out var sceneView))
            return null;

        return ScreenWorldConverter.ScreenToWorld2D(
            windowPosition,
            pointerSurface.Origin,
            pointerSurface.Size,
            sceneView.ViewProjection);
    }

    internal static bool TryGetPrimaryView(Context context, out SceneView view)
    {
        foreach (var (entity, cameraComponent) in context.View<CameraComponent>())
        {
            if (!cameraComponent.Primary)
                continue;

            var transform = cameraComponent.CameraViewTransform
                ?? (entity.TryGetComponent<TransformComponent>(out var transformComponent)
                    ? transformComponent.GetWorldTransform()
                    : Matrix4x4.Identity);

            Scratch.Apply(cameraComponent);
            return CameraViews.TryFrom(Scratch, transform, out view);
        }

        view = default;
        return false;
    }
}

[SkipUnitTests]
internal sealed class NullCameraQueries : ICameraQueries
{
    public static readonly NullCameraQueries Instance = new();

    public Vector2? ScreenToWorld2D(Vector2 windowPosition) => null;
}
