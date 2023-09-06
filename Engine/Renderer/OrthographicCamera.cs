using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public class OrthographicCamera
{
    public OrthographicCamera(float left, float right, float bottom, float top)
    {
        Position = Vector3.Zero;
        Rotation = 0.0f;
        Scale = Vector3.Zero;

        ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewMatrix = Matrix4.Identity;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    public Matrix4 ProjectionMatrix { get; private set; }
    public Matrix4 ViewMatrix { get; private set; }
    public Matrix4 ViewProjectionMatrix { get; private set; }
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

    private void RecalculateViewMatrix()
    {
        var position = Matrix4.CreateTranslation(Position.X, Position.Y, 0);
        var scale = Matrix4.CreateScale(Scale.X, Scale.Y, Scale.Z);
        var rotation = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation));

        var transform = Matrix4.Identity;
        transform *= position;
        transform *= rotation;
        
        ViewMatrix = Matrix4.Invert(transform);
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }
}