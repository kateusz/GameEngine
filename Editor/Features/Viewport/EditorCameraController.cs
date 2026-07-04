using System.Numerics;
using Engine.Renderer.Cameras;

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

        _camera.SetFocalPoint(Vector3.Zero);
        _camera.SetDistance(CameraConfig.DefaultEditorDistance);
        _camera.SetPitch(0.0f);
        _camera.SetYaw(0.0f);
    }
}
