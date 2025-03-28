using System.Numerics;
using Engine.Math;

namespace Engine.Renderer.Cameras;

public class OrthographicCamera
{
    public OrthographicCamera(float left, float right, float bottom, float top)
    {
        Position = Vector3.Zero;
        Rotation = 0.0f;
        Scale = Vector3.Zero;

        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewMatrix = Matrix4x4.Identity;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    public Matrix4x4 ProjectionMatrix { get; private set; }
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ViewProjectionMatrix { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public Vector3 Scale { get; private set; }

    public void SetPosition(Vector3 position)
    {
        Position = position;
        RecalculateViewMatrix();
    }

    public void SetRotation(float rotation)
    {
        Rotation = rotation;
        RecalculateViewMatrix();
    }

    public void SetScale(Vector3 scale)
    {
        Scale = scale;
        RecalculateViewMatrix();
    }

    public void SetProjection(float left, float right, float bottom, float top)
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    private void RecalculateViewMatrix()
    {
        var position = Matrix4x4.CreateTranslation(Position.X, Position.Y, 0);
        var rotation = Matrix4x4.CreateRotationZ(MathHelpers.DegreesToRadians(Rotation));
        var scale = Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z);

        var transform = Matrix4x4.Identity;
        transform *= position;
        transform *= rotation;

        Matrix4x4.Invert(transform, out var result);
        ViewMatrix = result;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }
}