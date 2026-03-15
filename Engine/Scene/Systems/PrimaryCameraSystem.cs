using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene.Systems;

internal sealed class PrimaryCameraSystem(IContext context) : ISystem, IPrimaryCameraProvider
{
    public int Priority => SystemPriorities.PrimaryCameraSystem;

    public Camera? Camera { get; private set; }
    public Matrix4x4 Transform { get; private set; } = Matrix4x4.Identity;

    private Entity? _cachedEntity;
    private CameraComponent? _cachedCameraComponent;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (_cachedEntity != null && _cachedCameraComponent?.Primary == true)
        {
            Transform = _cachedEntity.GetComponent<TransformComponent>().GetTransform();
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
            Camera = cameraComponent.Camera;
            Transform = entity.GetComponent<TransformComponent>().GetTransform();
            break;
        }
    }

    public void OnShutdown() { }
}
