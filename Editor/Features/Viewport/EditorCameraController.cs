using System.Numerics;
using Engine.Scene.Cameras;

namespace Editor.Features.Viewport;

public interface IEditorCameraController
{
    void SetCamera(EditorCamera camera);
    void ResetCamera();
}

public sealed class EditorCameraController : IEditorCameraController
{
    private EditorCamera? _camera;

    public void SetCamera(EditorCamera camera) => _camera = camera;

    public void ResetCamera()
    {
        if (_camera is null)
            return;

        _camera.FocalPoint = Vector3.Zero;
        _camera.Distance = CameraConfig.DefaultEditorDistance;
        _camera.Pitch = 0.0f;
        _camera.Yaw = 0.0f;
        _camera.ResetFlySpeedMultiplier();
    }
}
