using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Window;
using Engine.Scene.Cameras;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace Engine.Scene.Systems;

/// <summary>
/// Per-scene screen→world using that scene's primary camera and the host pointer surface.
/// </summary>
internal sealed class CameraQueries(IContext context, IPointerSurface pointerSurface) : ICameraQueries
{
    private readonly SceneCamera _scratchCamera = new();

    public Vector2? ScreenToWorld2D(Vector2 windowPosition)
    {
        if (!pointerSurface.Contains(windowPosition))
            return null;

        if (!TryGetPrimaryViewProjection(out var viewProjection))
            return null;

        return ScreenWorldConverter.ScreenToWorld2D(
            windowPosition,
            pointerSurface.Origin,
            pointerSurface.Size,
            viewProjection);
    }

    private bool TryGetPrimaryViewProjection(out Matrix4x4 viewProjection)
    {
        viewProjection = default;

        foreach (var (entity, cameraComponent) in context.View<CameraComponent>())
        {
            if (!cameraComponent.Primary)
                continue;

            var transform = cameraComponent.CameraViewTransform
                ?? (entity.TryGetComponent<TransformComponent>(out var transformComponent)
                    ? transformComponent.GetWorldTransform()
                    : Matrix4x4.Identity);

            if (!Matrix4x4.Invert(transform, out var viewMatrix))
                return false;

            ApplyComponentToScratch(cameraComponent);
            viewProjection = viewMatrix * _scratchCamera.GetProjectionMatrix();
            return true;
        }

        return false;
    }

    private void ApplyComponentToScratch(CameraComponent component)
    {
        _scratchCamera.ProjectionType = component.ProjectionType == CameraProjectionTypeData.Perspective
            ? ProjectionType.Perspective
            : ProjectionType.Orthographic;
        _scratchCamera.OrthographicSize = component.OrthographicSize;
        _scratchCamera.OrthographicNear = component.OrthographicNear;
        _scratchCamera.OrthographicFar = component.OrthographicFar;
        _scratchCamera.PerspectiveFOV = component.PerspectiveFOV;
        _scratchCamera.PerspectiveNear = component.PerspectiveNear;
        _scratchCamera.PerspectiveFar = component.PerspectiveFar;
        _scratchCamera.AspectRatio = component.AspectRatio;
    }
}

[SkipUnitTests]
internal sealed class NullCameraQueries : ICameraQueries
{
    public static readonly NullCameraQueries Instance = new();

    public Vector2? ScreenToWorld2D(Vector2 windowPosition) => null;
}
