using System.Numerics;
using Engine.Math;
using Serilog;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Orthographic camera implementation for 2D rendering.
/// Provides position, rotation, and scale controls with combined view-projection matrix.
/// </summary>
public class OrthographicCamera : Camera
{
    private static readonly ILogger Logger = Log.ForContext<OrthographicCamera>();

    public OrthographicCamera(float left, float right, float bottom, float top)
    {
        Position = Vector3.Zero;
        Rotation = 0.0f;
        Scale = Vector3.One;

        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -100.0f, 100.0f);
        ViewMatrix = Matrix4x4.Identity;
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    public Matrix4x4 ProjectionMatrix { get; private set; }
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ViewProjectionMatrix { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    public Vector3 Scale { get; private set; }

    /// <summary>
    /// Gets the projection matrix for this camera.
    /// </summary>
    public override Matrix4x4 GetProjectionMatrix() => ProjectionMatrix;

    /// <summary>
    /// Gets the view matrix for this camera.
    /// </summary>
    public override Matrix4x4 GetViewMatrix() => ViewMatrix;

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
        ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -100.0f, 100.0f);
        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }

    private void RecalculateViewMatrix()
    {
        // Build the transform matrix from position, rotation, and scale
        // Directly compose transforms without redundant identity matrix multiplication
        var transform = Matrix4x4.CreateScale(Scale) *
                        Matrix4x4.CreateTranslation(Position.X, Position.Y, Position.Z) *
                        Matrix4x4.CreateRotationZ(MathHelpers.DegreesToRadians(Rotation));

        // Invert the transform to get the view matrix
        if (!Matrix4x4.Invert(transform, out var result))
        {
            Logger.Error("Failed to invert camera transform matrix. Using identity matrix as fallback. " +
                         "Position: {Position}, Rotation: {Rotation}, Scale: {Scale}",
                         Position, Rotation, Scale);
            ViewMatrix = Matrix4x4.Identity;
        }
        else
        {
            ViewMatrix = result;
        }

        ViewProjectionMatrix = ProjectionMatrix * ViewMatrix;
    }
}