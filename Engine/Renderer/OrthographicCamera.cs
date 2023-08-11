using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public class OrthographicCamera
{
    public OrthographicCamera(float left, float right, float bottom, float top)
    {
        Position = new Vector3(0.0f, 0.0f, 0.0f);

        ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
        ViewMatrix = Matrix4.Identity;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    public Matrix4 ProjectionMatrix { get; private set; }
    public Matrix4 ViewMatrix { get; private set; }
    public Matrix4 ViewProjectionMatrix { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }

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

    private void RecalculateViewMatrix()
    {
        var transform = Matrix4.Identity;

        // Apply translation
        transform *= Matrix4.CreateTranslation(Position);

        // Apply rotation
        //transform *= Matrix4.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(Rotation));

        ViewMatrix = transform.Inverted();
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }
}