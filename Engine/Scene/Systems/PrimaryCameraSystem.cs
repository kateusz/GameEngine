using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer.Cameras;
using SceneComponents;
using SceneComponents.Camera;

namespace Engine.Scene.Systems;

internal sealed class PrimaryCameraSystem(IContext context) : ISystem, IPrimaryCameraProvider
{
    public int Priority => SystemPriorities.PrimaryCameraSystem;

    public Camera? Camera { get; private set; }
    public Matrix4x4 Transform { get; private set; } = Matrix4x4.Identity;

    private Entity? _cachedEntity;
    private CameraComponent? _cachedCameraComponent;
    private readonly Dictionary<int, SceneCamera> _runtimeCameras = [];

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (_cachedEntity != null && _cachedCameraComponent?.Primary == true)
        {
            Camera = ResolveRuntimeCamera(_cachedEntity.Id, _cachedCameraComponent);
            Transform = _cachedCameraComponent.CameraViewTransform
                ?? (_cachedEntity.TryGetComponent<TransformComponent>(out var transform)
                    ? transform.GetTransform()
                    : Matrix4x4.Identity);
            return;
        }

        Camera = null;
        Transform = Matrix4x4.Identity;
        _cachedEntity = null;
        _cachedCameraComponent = null;

        foreach (var (entity, cameraComponent) in context.View<CameraComponent>())
        {
            if (!cameraComponent.Primary)
                continue;

            _cachedEntity = entity;
            _cachedCameraComponent = cameraComponent;
            Camera = ResolveRuntimeCamera(entity.Id, cameraComponent);
            Transform = cameraComponent.CameraViewTransform
                ?? (entity.TryGetComponent<TransformComponent>(out var transform)
                    ? transform.GetTransform()
                    : Matrix4x4.Identity);
            break;
        }
    }

    public void OnShutdown()
    {
        Camera = null;
        Transform = Matrix4x4.Identity;
        _cachedEntity = null;
        _cachedCameraComponent = null;
        _runtimeCameras.Clear();
    }

    private SceneCamera ResolveRuntimeCamera(int entityId, CameraComponent component)
    {
        if (!_runtimeCameras.TryGetValue(entityId, out var camera))
        {
            camera = new SceneCamera();
            _runtimeCameras[entityId] = camera;
        }

        camera.ProjectionType = component.ProjectionType == CameraProjectionTypeData.Perspective
            ? ProjectionType.Perspective
            : ProjectionType.Orthographic;
        camera.OrthographicSize = component.OrthographicSize;
        camera.OrthographicNear = component.OrthographicNear;
        camera.OrthographicFar = component.OrthographicFar;
        camera.PerspectiveFOV = component.PerspectiveFOV;
        camera.PerspectiveNear = component.PerspectiveNear;
        camera.PerspectiveFar = component.PerspectiveFar;
        camera.AspectRatio = component.AspectRatio;
        return camera;
    }
}
