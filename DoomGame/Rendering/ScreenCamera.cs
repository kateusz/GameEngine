using System.Numerics;
using Engine.Renderer.Cameras;

namespace DoomGame.Rendering;

public class ScreenCamera : IViewCamera
{
    private float _width;
    private float _height;

    public ScreenCamera(float width, float height)
    {
        _width = width;
        _height = height;
    }

    public Matrix4x4 GetViewProjectionMatrix() =>
        Matrix4x4.CreateOrthographic(_width, _height, -1f, 1f);

    public Vector3 GetPosition() => Vector3.Zero;

    public void Resize(float width, float height)
    {
        _width = width;
        _height = height;
    }
}
