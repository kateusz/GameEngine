using ECS;
using Engine.Scene;
using Engine.Scene.Cameras;

namespace Editor.Features.Viewport;

internal sealed class EditorCameraFramingService(ISceneContext sceneContext) : IEditorCameraFraming
{
    private EditorCamera? _camera;

    public void SetCamera(EditorCamera camera) => _camera = camera;

    public void FocusOnEntity(Entity entity, bool resetDistance = false)
    {
        if (_camera is null)
            return;

        var scene = sceneContext.ActiveScene;
        if (scene is null || !scene.Context.Contains(entity.Id))
            return;

        var resolved = scene.Context.GetById(entity.Id);
        scene.UpdateWorldTransforms();
        _camera.FocalPoint = scene.GetWorldPosition(resolved);

        if (resetDistance)
            _camera.Distance = CameraConfig.DefaultEditorDistance;
    }
}
