using ECS;
using Engine.Scene;
using Engine.Scene.Cameras;
using SceneComponents;
using Serilog;

namespace Editor.Features.Viewport;

internal sealed class EditorCameraFramingService(ISceneContext sceneContext) : IEditorCameraFraming
{
    private static readonly ILogger Logger = Log.ForContext<EditorCameraFramingService>();

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
        var localBefore = resolved.TryGetComponent<TransformComponent>(out var t0)
            ? t0.Translation
            : (System.Numerics.Vector3?)null;
        var worldBefore = resolved.TryGetComponent<TransformComponent>(out var t1)
            ? t1.GetWorldTransform().Translation
            : (System.Numerics.Vector3?)null;

        scene.UpdateWorldTransforms();
        var world = scene.GetWorldPosition(resolved);
        var parent = scene.GetParent(resolved);

        // ponytail: temporary focus diagnostics — remove once child-vs-root framing is confirmed
        Logger.Warning(
            "FocusOnEntity id={Id} name={Name} parent={Parent} local={Local} worldBefore={WorldBefore} worldAfter={World} focalBefore={FocalBefore} resetDistance={Reset}",
            resolved.Id,
            resolved.Name,
            parent?.Name ?? "(root)",
            localBefore,
            worldBefore,
            world,
            _camera.FocalPoint,
            resetDistance);

        _camera.FocalPoint = world;

        if (resetDistance)
            _camera.Distance = CameraConfig.DefaultEditorDistance;
    }
}
