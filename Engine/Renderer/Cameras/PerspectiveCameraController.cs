using System.Numerics;

namespace Engine.Renderer.Cameras;

public class PerspectiveCameraController
{
    private readonly PerspectiveCamera _camera;
    private bool _rightMouseDown;
    private bool _middleMouseDown;

    private const int RightMouseButton = 1;
    private const int MiddleMouseButton = 2;

    public PerspectiveCameraController(PerspectiveCamera camera)
    {
        _camera = camera;
    }

    public PerspectiveCamera Camera => _camera;

    public void OnMouseButtonPressed(int button)
    {
        if (button == RightMouseButton) _rightMouseDown = true;
        else if (button == MiddleMouseButton) _middleMouseDown = true;
    }

    public void OnMouseButtonReleased(int button)
    {
        if (button == RightMouseButton) _rightMouseDown = false;
        else if (button == MiddleMouseButton) _middleMouseDown = false;
    }

    public void OnMouseMoved(Vector2 position)
    {
        _camera.OnMouseMove(position, pan: _middleMouseDown, orbit: _rightMouseDown, zoomDrag: false);
    }

    public void OnMouseScroll(float yOffset) => _camera.OnMouseScroll(yOffset);

    public void SetViewportSize(float width, float height) => _camera.SetViewportSize(width, height);
}
